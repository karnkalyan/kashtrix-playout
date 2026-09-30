using System.Diagnostics;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Engine;

/// <summary>
/// Single-channel playout coordinator with composite/multipart event support.
/// A PlaylistItem may contain many trim parts that all reference the same source file.
/// The parts are decoded in order and appear to automation as one continuous event.
/// </summary>
public sealed class PlayoutEngine : IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private long _sessionId;
    private readonly ManualResetEventSlim _pauseGate = new(initialState: true);
    private bool _isPaused;
    private VideoFrameData? _lastVideoFrame;
    private bool _disposed;
    private readonly Lazy<EnterpriseMamCatalog> _mamCatalog = new(() => new EnterpriseMamCatalog());

    public event Action<VideoFrameData>? VideoFrame;
    public event Action<AudioChunk>? AudioFrame;
    // Raw programme-source audio used only for pre-fader/pre-mute metering.
    // This remains active even when an item or the master bus is muted.
    public event Action<AudioChunk>? AudioMeterFrame;
    public event Action<PlaylistItem?>? CurrentItemChanged;
    public event Action<TimeSpan>? ProgressChanged;
    public event Action<string>? StatusChanged;
    public event Action<PlaylistItem>? ControlEventTriggered;

    public bool IsPlaying { get { lock (_sync) return _runTask is { IsCompleted: false }; } }
    public bool IsPaused { get { lock (_sync) return _isPaused && _runTask is { IsCompleted: false }; } }

    /// <param name="firstItemOffset">Offset inside the logical event, not an absolute source-media timestamp.</param>
    public Task PlayAsync(IReadOnlyList<PlaylistItem> items, int startIndex, TimeSpan? firstItemOffset = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PlayoutEngine));
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) return Task.CompletedTask;

        CancellationTokenSource source;
        Task task;
        long session;
        lock (_sync)
        {
            CancelCurrentSource_NoThrow();
            _isPaused = false;
            _pauseGate.Set();
            source = new CancellationTokenSource();
            _cts = source;
            session = ++_sessionId;
            var snapshot = items.ToArray();
            var safeIndex = Math.Clamp(startIndex, 0, items.Count - 1);
            task = Task.Run(() => RunPlaylistAsync(snapshot, safeIndex, firstItemOffset, session, source.Token));
            _runTask = task;
        }
        _ = CleanupSessionAsync(task, source, session);
        return task;
    }

    private async Task CleanupSessionAsync(Task task, CancellationTokenSource source, long session)
    {
        try { await task.ConfigureAwait(false); }
        catch (Exception ex) { SetStatus(session, "ERROR: " + ex.Message); }
        finally
        {
            lock (_sync)
            {
                // Always clear the CTS reference that belongs to this source before disposing it.
                // Stop() advances _sessionId; the old FIX39 condition also required the old session
                // id to match, which could leave _cts pointing at an already-disposed source.
                if (ReferenceEquals(_cts, source)) _cts = null;
                if (_sessionId == session && ReferenceEquals(_runTask, task)) _runTask = null;
            }
            source.Dispose();
        }
    }

    private bool IsCurrentSession(long session) { lock (_sync) return _sessionId == session; }

    // Output/UI subscribers must never be able to terminate the decoder loop. A graphics,
    // NDI, monitor or UI presentation exception is isolated to that subscriber/frame.
    private static void InvokeSafely<T>(Action<T>? handlers, T value)
    {
        if (handlers is null) return;
        foreach (Action<T> handler in handlers.GetInvocationList())
        {
            try { handler(value); } catch { }
        }
    }

    private void SetStatus(long session, string status) { if (IsCurrentSession(session)) InvokeSafely(StatusChanged, status); }
    private void SetCurrentItem(long session, PlaylistItem? item) { if (IsCurrentSession(session)) InvokeSafely(CurrentItemChanged, item); }
    private void SetProgress(long session, TimeSpan progress) { if (IsCurrentSession(session)) InvokeSafely(ProgressChanged, progress < TimeSpan.Zero ? TimeSpan.Zero : progress); }
    private void EmitVideo(VideoFrameData frame) => InvokeSafely(VideoFrame, frame);
    private void EmitAudio(AudioChunk chunk) => InvokeSafely(AudioFrame, chunk);
    private void EmitAudioMeter(AudioChunk chunk) => InvokeSafely(AudioMeterFrame, chunk);
    private void EmitControl(PlaylistItem item) => InvokeSafely(ControlEventTriggered, item);

    private async Task RunPlaylistAsync(PlaylistItem[] items, int index, TimeSpan? firstItemOffset, long session, CancellationToken ct)
    {
        try
        {
            SetStatus(session, "PLAYING");
            for (var i = index; i < items.Length; i++)
            {
                if (ct.IsCancellationRequested || !IsCurrentSession(session)) return;
                var item = items[i];
                SetCurrentItem(session, item);
                var offset = i == index && firstItemOffset.HasValue ? ClampOffset(item, firstItemOffset.Value) : TimeSpan.Zero;
                SetProgress(session, offset);
                var completed = await PlayItemAsync(item, offset, session, ct).ConfigureAwait(false);
                if (!completed || ct.IsCancellationRequested || !IsCurrentSession(session)) return;

                if (item.IsLoop)
                {
                    if (item.LoopCount <= 0 || item.CurrentLoopIteration < item.LoopCount - 1)
                    {
                        item.CurrentLoopIteration++;
                        i--;
                        continue;
                    }
                    item.CurrentLoopIteration = 0;
                }
                else if (item.IsBlockLoop)
                {
                    var nextIdx = i + 1;
                    // BlockName is a marker on the first row. Following rows inherit it,
                    // so the next marker (or rundown end) is the real end of the block.
                    var isEndOfBlock = nextIdx >= items.Length || !string.IsNullOrWhiteSpace(items[nextIdx].BlockName);
                    if (isEndOfBlock)
                    {
                        var blockStart = i;
                        while (blockStart > 0 && string.IsNullOrWhiteSpace(items[blockStart].BlockName)) blockStart--;
                        i = blockStart - 1;
                        continue;
                    }
                }
            }
            SetStatus(session, "READY");
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested && IsCurrentSession(session)) SetStatus(session, "ERROR: " + ex.Message);
        }
        finally { SetCurrentItem(session, null); }
    }

    private static TimeSpan ClampOffset(PlaylistItem item, TimeSpan requested)
    {
        if (requested < TimeSpan.Zero) return TimeSpan.Zero;
        if (item.IsLiveSource) return TimeSpan.Zero;
        if (item.Duration <= TimeSpan.Zero) return TimeSpan.Zero;
        if (requested >= item.Duration)
        {
            var safe = item.Duration - TimeSpan.FromMilliseconds(40);
            return safe < TimeSpan.Zero ? TimeSpan.Zero : safe;
        }
        return requested;
    }

    private async Task<bool> PlayItemAsync(PlaylistItem item, TimeSpan eventOffset, long session, CancellationToken ct)
    {
        // Enforce MAM policy at the actual playout boundary, not only in UI. This also protects
        // auto-follow events and remote API/automation TAKE commands. If an online master was
        // archived/offlined but an archive copy is available, restore it automatically before air.
        if (!item.IsLiveSource && !item.IsControlEvent && !item.IsCgEvent && !string.IsNullOrWhiteSpace(item.FilePath))
        {
            var mam = _mamCatalog.Value;
            var asset = mam.GetByPath(item.FilePath);
            if (asset is not null)
            {
                mam.AssertMayUse(item.FilePath, DateTime.UtcNow);
                if (!File.Exists(item.FilePath) && !string.IsNullOrWhiteSpace(asset.ArchivePath) && File.Exists(asset.ArchivePath))
                    await mam.RestoreAsync(asset.AssetId, item.FilePath, ct).ConfigureAwait(false);
            }
        }

        if (item.IsControlEvent)
            return await PlayControlEventAsync(item, session, ct).ConfigureAwait(false);

        if (item.IsCgEvent)
            return await PlayCgEventAsync(item, eventOffset, session, ct).ConfigureAwait(false);

        var parts = item.GetPlaybackParts();
        if (parts.Count == 0) return true;

        var startPartIndex = 0;
        var sourceStart = parts[0].InPoint;
        if (!item.IsLiveSource)
        {
            var mapped = item.MapEventOffset(eventOffset);
            startPartIndex = Math.Max(0, parts.ToList().FindIndex(x => x.Index == mapped.Part.Index));
            sourceStart = mapped.SourcePosition;
        }

        for (var i = startPartIndex; i < parts.Count; i++)
        {
            if (ct.IsCancellationRequested || !IsCurrentSession(session)) return false;
            var part = parts[i];
            var start = i == startPartIndex ? sourceStart : part.InPoint;
            var completed = await PlaySegmentAsync(item, part, start, session, ct).ConfigureAwait(false);
            if (!completed) return false;
        }
        return !ct.IsCancellationRequested && IsCurrentSession(session);
    }

    private async Task<bool> PlaySegmentAsync(PlaylistItem item, MediaPlaybackPart part, TimeSpan startAt, long session, CancellationToken ct)
    {
        using var video = new VideoDecoder(item);
        AuditLogService.Write("DECODER_OPEN", item.Title, $"{item.DecoderPreference} -> {video.DecoderBackend}");
        if (!item.IsLiveSource) video.Seek(startAt);

        AudioDecoder? audio = null;
        var sourceHasConfiguredAudio = !item.SourceKind.Equals("Screen", StringComparison.OrdinalIgnoreCase)
            && (!item.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(item.AudioDevice));
        if (sourceHasConfiguredAudio)
        {
            try { audio = new AudioDecoder(item); if (!item.IsLiveSource) audio.Seek(startAt); }
            catch (Exception ex)
            {
                audio?.Dispose(); audio = null;
                AuditLogService.Write("AUDIO_DECODER_ERROR", item.Title, ex.Message);
                SetStatus(session, "AUDIO DECODER: " + ex.Message);
            }
        }

        var audioStop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var audioTask = audio is null ? Task.CompletedTask : Task.Run(() => PumpAudio(audio, item, part, startAt, session, audioStop.Token));

        try
        {
            var clock = Stopwatch.StartNew();
            var firstPts = double.NaN;
            var pausedSeconds = 0.0;
            while (!ct.IsCancellationRequested && IsCurrentSession(session))
            {
                pausedSeconds += WaitWhilePaused(session, ct);
                if (ct.IsCancellationRequested || !IsCurrentSession(session)) return false;
                if (!video.TryRead(out var frame)) break;
                if (!item.IsLiveSource && frame.PtsSeconds < startAt.TotalSeconds - 0.050) continue;
                if (double.IsNaN(firstPts)) firstPts = frame.PtsSeconds;

                double segmentElapsed;
                if (item.IsLiveSource)
                {
                    segmentElapsed = Math.Max(0, frame.PtsSeconds - firstPts);
                    if (item.Duration > TimeSpan.Zero && segmentElapsed >= item.Duration.TotalSeconds) break;
                }
                else
                {
                    if (frame.PtsSeconds >= part.OutPoint.TotalSeconds) break;
                    segmentElapsed = Math.Max(0, frame.PtsSeconds - part.InPoint.TotalSeconds);
                }

                var target = Math.Max(0, frame.PtsSeconds - firstPts);
                while (!ct.IsCancellationRequested && IsCurrentSession(session))
                {
                    pausedSeconds += WaitWhilePaused(session, ct);
                    var activeElapsed = clock.Elapsed.TotalSeconds - pausedSeconds;
                    var delay = target - activeElapsed;
                    // Program used to sleep for the whole remaining frame interval. At 50/60 fps
                    // a normal Windows scheduler overshoot was enough to create visible cadence
                    // jitter. Sleep coarsely first, then finish against the Stopwatch deadline.
                    if (delay <= 0.0015) break;
                    if (delay > 0.006)
                    {
                        var coarseMs = Math.Max(1.0, Math.Min(5.0, (delay - 0.003) * 1000.0));
                        await Task.Delay(TimeSpan.FromMilliseconds(coarseMs)).ConfigureAwait(false);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                if (ct.IsCancellationRequested || !IsCurrentSession(session)) return false;

                SetProgress(session, item.IsLiveSource ? TimeSpan.FromSeconds(segmentElapsed) : part.EventOffset + TimeSpan.FromSeconds(segmentElapsed));
                _lastVideoFrame = frame;
                EmitVideo(frame);
            }
            return !ct.IsCancellationRequested && IsCurrentSession(session);
        }
        finally
        {
            audioStop.Cancel();
            if (audio is null)
            {
                audioStop.Dispose();
            }
            else
            {
                // Some capture/network demuxers can remain inside av_read_frame after video
                // has already completed. Do not hold the whole Program playlist forever
                // waiting for an audio tail that ignored cancellation.
                var finished = await Task.WhenAny(audioTask, Task.Delay(750)).ConfigureAwait(false);
                if (ReferenceEquals(finished, audioTask))
                {
                    try { await audioTask.ConfigureAwait(false); }
                    catch (Exception ex) { if (!ct.IsCancellationRequested && IsCurrentSession(session)) SetStatus(session, "AUDIO ERROR: " + ex.Message); }
                    audio.Dispose();
                    audioStop.Dispose();
                }
                else
                {
                    try { audio.Dispose(); } catch { }
                    _ = audioTask.ContinueWith(t =>
                    {
                        try { _ = t.Exception; } catch { }
                        try { audioStop.Dispose(); } catch { }
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
        }
    }

    private void PumpAudio(AudioDecoder audio, PlaylistItem item, MediaPlaybackPart part, TimeSpan startAt, long session, CancellationToken ct)
    {
        try
        {
            var clock = Stopwatch.StartNew();
            var firstPts = double.NaN;
            var pausedSeconds = 0.0;
            while (!ct.IsCancellationRequested && IsCurrentSession(session))
            {
                pausedSeconds += WaitWhilePaused(session, ct);
                if (ct.IsCancellationRequested || !IsCurrentSession(session)) return;
                if (!audio.TryRead(out var chunk)) return;
                if (!item.IsLiveSource && chunk.PtsSeconds < startAt.TotalSeconds - 0.100) continue;
                if (double.IsNaN(firstPts)) firstPts = chunk.PtsSeconds;
                if (item.IsLiveSource)
                {
                    if (item.Duration > TimeSpan.Zero && chunk.PtsSeconds - firstPts >= item.Duration.TotalSeconds) return;
                }
                else if (chunk.PtsSeconds >= part.OutPoint.TotalSeconds) return;

                var target = Math.Max(0, chunk.PtsSeconds - firstPts + item.AudioDelayMs / 1000.0);
                while (!ct.IsCancellationRequested && IsCurrentSession(session))
                {
                    pausedSeconds += WaitWhilePaused(session, ct);
                    var activeElapsed = clock.Elapsed.TotalSeconds - pausedSeconds;
                    var wait = target - activeElapsed;
                    if (wait <= 0.001) break;
                    if (ct.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(Math.Min(20.0, wait * 1000.0)))) return;
                }
                if (ct.IsCancellationRequested || !IsCurrentSession(session)) return;
                EmitAudioMeter(chunk);
                EmitAudio(AudioMasterProcessor.Apply(chunk, item.AudioGainPercent, item.AudioMuted));
            }
        }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested && IsCurrentSession(session)) SetStatus(session, "AUDIO ERROR: " + ex.Message);
        }
    }

    private async Task<bool> PlayControlEventAsync(PlaylistItem item, long session, CancellationToken ct)
    {
        var action = (item.EventType ?? "NOTE").Trim().ToUpperInvariant();
        SetProgress(session, TimeSpan.Zero);
        EmitControl(item);
        switch (action)
        {
            case "STOP":
                SetStatus(session, "STOPPED · CONTROL EVENT");
                await Task.Delay(40).ConfigureAwait(false);
                return false;
            case "PAUSE":
                Pause();
                WaitWhilePaused(session, ct);
                return !ct.IsCancellationRequested && IsCurrentSession(session);
            case "RESUME":
                Resume();
                return true;
            case "CUETONE":
                await EmitToneAsync(1000, 0, 850, session, ct, .34).ConfigureAwait(false);
                return !ct.IsCancellationRequested && IsCurrentSession(session);
            case "DIALTONE":
                await EmitToneAsync(350, 440, item.DialToneMilliseconds, session, ct, item.DialToneLevelPercent / 100.0).ConfigureAwait(false);
                return !ct.IsCancellationRequested && IsCurrentSession(session);
            case "DTMF":
                await EmitDtmfAsync(item.DtmfSequence, item.DtmfToneMilliseconds, item.DtmfGapMilliseconds, item.DtmfLevelPercent, session, ct).ConfigureAwait(false);
                return !ct.IsCancellationRequested && IsCurrentSession(session);
            case "NEXT":
            case "PLAY":
            case "NOTE":
            default:
                await Task.Delay(40).ConfigureAwait(false);
                return !ct.IsCancellationRequested && IsCurrentSession(session);
        }
    }

    private async Task EmitToneAsync(double f1, double f2, int milliseconds, long session, CancellationToken ct, double level = .275)
    {
        await EmitToneCoreAsync(f1, f2, milliseconds, Math.Clamp(level, .01, 1.0), ct,
            () => IsCurrentSession(session)).ConfigureAwait(false);
    }

    private async Task EmitToneCoreAsync(double f1, double f2, int milliseconds, double level, CancellationToken ct, Func<bool>? keepRunning = null)
    {
        const int sampleRate = 48000;
        const int chunkMs = 20;
        var total = Math.Max(1, (int)Math.Ceiling(Math.Max(20, milliseconds) / (double)chunkMs));
        var amplitude = Math.Clamp(level, .01, 1.0) * short.MaxValue * .92;
        for (var c = 0; c < total && !ct.IsCancellationRequested && (keepRunning?.Invoke() ?? true); c++)
        {
            var samples = sampleRate * chunkMs / 1000;
            var pcm = new byte[samples * 4];
            for (var i = 0; i < samples; i++)
            {
                var t = (c * samples + i) / (double)sampleRate;
                var wave = Math.Sin(2 * Math.PI * f1 * t);
                if (f2 > 0) wave = (wave + Math.Sin(2 * Math.PI * f2 * t)) * .5;
                // A short edge ramp avoids clicks when operators test DTMF/dial tone on air.
                var absoluteSample = c * samples + i;
                var totalSamples = total * samples;
                var ramp = Math.Min(1.0, Math.Min(absoluteSample / (sampleRate * .006), (totalSamples - absoluteSample) / (sampleRate * .006)));
                var sample = (short)Math.Clamp((int)(wave * amplitude * Math.Max(0, ramp)), short.MinValue, short.MaxValue);
                pcm[i * 4] = (byte)(sample & 0xff); pcm[i * 4 + 1] = (byte)((sample >> 8) & 0xff);
                pcm[i * 4 + 2] = pcm[i * 4]; pcm[i * 4 + 3] = pcm[i * 4 + 1];
            }
            var toneChunk = new AudioChunk(pcm, c * chunkMs / 1000.0);
            EmitAudioMeter(toneChunk);
            EmitAudio(toneChunk);
            await Task.Delay(chunkMs, ct).ConfigureAwait(false);
        }
    }

    private async Task EmitDialToneAsync(string? notes, long session, CancellationToken ct)
    {
        // North-American/commonly-recognised dial tone: 350 Hz + 440 Hz. Notes may override
        // duration and level: "DIALTONE: duration=3000;level=55".
        var duration = ParseOptionInt(notes, "duration", 2500, 100, 30000);
        var levelPercent = ParseOptionInt(notes, "level", 55, 1, 100);
        await EmitToneAsync(350, 440, duration, session, ct, levelPercent / 100.0).ConfigureAwait(false);
    }

    private async Task EmitDtmfAsync(string? sequence, int toneMs, int gapMs, int levelPercent, long session, CancellationToken ct)
    {
        var digits = new string((sequence ?? "1234#").Where(ch => "0123456789*#ABCDabcd".Contains(ch)).Select(char.ToUpperInvariant).ToArray());
        if (digits.Length == 0) digits = "1234#";
        toneMs = Math.Clamp(toneMs, 45, 2000);
        gapMs = Math.Clamp(gapMs, 20, 2000);
        levelPercent = Math.Clamp(levelPercent, 1, 100);
        foreach (var d in digits)
        {
            var (low, high) = DtmfFrequencies(d);
            await EmitToneAsync(low, high, toneMs, session, ct, levelPercent / 100.0).ConfigureAwait(false);
            if (gapMs > 0) await Task.Delay(gapMs, ct).ConfigureAwait(false);
        }
    }

    public async Task PlayDiagnosticToneAsync(double f1, double f2, int milliseconds, double level = .5, CancellationToken ct = default)
    {
        await EmitToneCoreAsync(f1, f2, Math.Clamp(milliseconds, 50, 30000), Math.Clamp(level, .01, 1.0), ct).ConfigureAwait(false);
    }

    public async Task PlayDiagnosticDtmfAsync(string? sequence, int toneMilliseconds = 120, int gapMilliseconds = 55, double level = .72, CancellationToken ct = default)
    {
        var digits = new string((sequence ?? "1234#").Where(ch => "0123456789*#ABCDabcd".Contains(ch)).Select(char.ToUpperInvariant).ToArray());
        if (digits.Length == 0) digits = "1234#";
        toneMilliseconds = Math.Clamp(toneMilliseconds, 45, 2000);
        gapMilliseconds = Math.Clamp(gapMilliseconds, 20, 2000);
        foreach (var d in digits)
        {
            var (low, high) = DtmfFrequencies(d);
            await EmitToneCoreAsync(low, high, toneMilliseconds, Math.Clamp(level, .01, 1.0), ct).ConfigureAwait(false);
            if (gapMilliseconds > 0) await Task.Delay(gapMilliseconds, ct).ConfigureAwait(false);
        }
    }

    private static int ParseOptionInt(string? text, string key, int fallback, int min, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        foreach (var part in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            if (!part[..eq].Trim().Equals(key, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(part[(eq + 1)..].Trim(), out var value)) return Math.Clamp(value, min, max);
        }
        return fallback;
    }

    private static (double Low, double High) DtmfFrequencies(char c) => c switch
    {
        '1' => (697,1209), '2' => (697,1336), '3' => (697,1477), 'A' => (697,1633),
        '4' => (770,1209), '5' => (770,1336), '6' => (770,1477), 'B' => (770,1633),
        '7' => (852,1209), '8' => (852,1336), '9' => (852,1477), 'C' => (852,1633),
        '*' => (941,1209), '0' => (941,1336), '#' => (941,1477), 'D' => (941,1633),
        _ => (697,1209)
    };

    private async Task<bool> PlayCgEventAsync(PlaylistItem item, TimeSpan eventOffset, long session, CancellationToken ct)
    {
        var duration = item.Duration > TimeSpan.Zero ? item.Duration : TimeSpan.FromSeconds(10);
        var start = eventOffset < TimeSpan.Zero ? TimeSpan.Zero : eventOffset > duration ? duration : eventOffset;
        var fps = item.SourceFrameRate > 0 ? Math.Clamp(item.SourceFrameRate, 1, 60) : 25.0;
        var frameDelay = TimeSpan.FromSeconds(1.0 / fps);
        var baseFrame = _lastVideoFrame ?? CreateBlackFrame(1280, 720, fps);
        var clock = Stopwatch.StartNew();
        var pausedSeconds = 0.0;

        while (!ct.IsCancellationRequested && IsCurrentSession(session))
        {
            pausedSeconds += WaitWhilePaused(session, ct);
            if (ct.IsCancellationRequested || !IsCurrentSession(session)) return false;

            var elapsed = start.TotalSeconds + Math.Max(0, clock.Elapsed.TotalSeconds - pausedSeconds);
            if (elapsed >= duration.TotalSeconds) break;
            SetProgress(session, TimeSpan.FromSeconds(elapsed));
            var frame = new VideoFrameData(baseFrame.Bgra, baseFrame.Width, baseFrame.Height, baseFrame.Stride, elapsed, baseFrame.FrameRateNumerator, baseFrame.FrameRateDenominator);
            EmitVideo(frame);
            await Task.Delay(frameDelay).ConfigureAwait(false);
        }
        return !ct.IsCancellationRequested && IsCurrentSession(session);
    }

    private static VideoFrameData CreateBlackFrame(int width, int height, double fps)
    {
        var stride = width * 4;
        var numerator = Math.Max(1, (int)Math.Round(fps * 1000));
        return new VideoFrameData(new byte[stride * height], width, height, stride, 0, numerator, 1000);
    }

    private double WaitWhilePaused(long session, CancellationToken ct)
    {
        if (_pauseGate.IsSet) return 0;
        var wait = Stopwatch.StartNew();
        try { _pauseGate.Wait(ct); }
        catch (OperationCanceledException) { return 0; }
        return IsCurrentSession(session) ? wait.Elapsed.TotalSeconds : 0;
    }

    public void Pause()
    {
        long session;
        lock (_sync)
        {
            if (_runTask is null || _runTask.IsCompleted || _isPaused) return;
            _isPaused = true;
            _pauseGate.Reset();
            session = _sessionId;
        }
        SetStatus(session, "PAUSED");
    }

    public void Resume()
    {
        long session;
        lock (_sync)
        {
            if (_runTask is null || _runTask.IsCompleted || !_isPaused) return;
            _isPaused = false;
            _pauseGate.Set();
            session = _sessionId;
        }
        SetStatus(session, "PLAYING");
    }

    public void TogglePause()
    {
        if (IsPaused) Resume(); else Pause();
    }

    private void CancelCurrentSource_NoThrow()
    {
        var source = _cts;
        if (source is null) return;
        try { source.Cancel(); }
        catch (ObjectDisposedException)
        {
            // A completed session may race its asynchronous cleanup. Never let an operator
            // TAKE/PLAY key turn that harmless race into an unhandled dispatcher exception.
            if (ReferenceEquals(_cts, source)) _cts = null;
        }
    }

    public void Stop()
    {
        long session;
        lock (_sync)
        {
            CancelCurrentSource_NoThrow();
            _isPaused = false;
            _pauseGate.Set();
            session = ++_sessionId;
        }
        SetStatus(session, "STOPPED"); SetCurrentItem(session, null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
