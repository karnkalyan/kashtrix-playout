using System;
using System.Threading;
using System.Threading.Tasks;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
using BroadcastPlayout.Outputs;

namespace Kashtrix.OutputEngine.Services;

/// <summary>
/// Central shared ingest producer for custom input sources (DirectShow capture devices,
/// NDI feeds, RTSP streams, screen grab, and media files).
/// Multiplexes a single physical hardware capture into a shared memory bus so multiple
/// broadcast outputs (DeckLink, NDI, UDP TS, RTMP) and the confidence monitor can all
/// stream the custom source concurrently without device lock collisions or duplicate decoding.
/// </summary>
public sealed class CustomInputHub : IDisposable
{
    private static readonly Lazy<CustomInputHub> _instance = new(() => new CustomInputHub());
    public static CustomInputHub Instance => _instance.Value;

    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private PlaylistItem? _currentItem;
    private static readonly Lazy<VirtualOutputBridge> _bridge = new(() => new VirtualOutputBridge(VirtualOutputBridge.CustomPreviewVideoMapName, VirtualOutputBridge.CustomPreviewAudioMapName));

    public PlaylistItem? CurrentItem
    {
        get { lock (_sync) return _currentItem; }
    }

    public bool IsRunning
    {
        get { lock (_sync) return _worker != null && !_worker.IsCompleted; }
    }

    public void SetInput(PlaylistItem item)
    {
        lock (_sync)
        {
            if (_currentItem != null && IsSameSource(_currentItem, item) && IsRunning)
            {
                return;
            }
            _currentItem = item;
            RestartWorkerLocked();
        }
    }

    public void EnsureRunning(PlaylistItem? fallbackItem = null)
    {
        lock (_sync)
        {
            if (IsRunning) return;
            if (_currentItem == null && fallbackItem != null)
            {
                _currentItem = fallbackItem;
            }
            if (_currentItem != null)
            {
                RestartWorkerLocked();
            }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            StopWorkerLocked();
        }
    }

    private void RestartWorkerLocked()
    {
        StopWorkerLocked();
        if (_currentItem == null) return;

        var item = _currentItem;
        var cts = new CancellationTokenSource();
        _cts = cts;
        _worker = Task.Run(() => WorkerLoopAsync(item, cts.Token));
    }

    private void StopWorkerLocked()
    {
        var cts = _cts;
        _cts = null;
        if (cts != null)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
        }
        _worker = null;
    }

    private async Task WorkerLoopAsync(PlaylistItem item, CancellationToken ct)
    {
        Scte35DvbService.Instance.Log("INPUT", $"Custom Ingest Hub started for '{item.Title}' [{item.SourceKind}]", "INFO");
        try
        {
            while (!ct.IsCancellationRequested)
            {
                using var decoder = new VideoDecoder(item);
                var fps = item.SourceFrameRate > 0 ? item.SourceFrameRate : 25.0;
                var frameInterval = TimeSpan.FromSeconds(1.0 / Math.Clamp(fps, 1, 120));
                bool hasFrames = false;

                while (!ct.IsCancellationRequested && decoder.TryRead(out var frame))
                {
                    hasFrames = true;
                    if (!_bridge.Value.IsOpen)
                    {
                        _bridge.Value.Open(frame.Width, frame.Height, frame.FramesPerSecond > 0 ? frame.FramesPerSecond : fps);
                    }
                    _bridge.Value.SubmitVideo(frame, onAir: true);

                    if (!item.IsLiveSource)
                    {
                        await Task.Delay(frameInterval, ct).ConfigureAwait(false);
                    }
                }

                if (item.IsLiveSource)
                {
                    if (!hasFrames)
                    {
                        Scte35DvbService.Instance.Log("INPUT_FAULT", $"Custom source '{item.Title}' produced no video frames. Retrying in 1s...", "WARN");
                        await Task.Delay(1000, ct).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(250, ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    // Loop VOD files continuously
                    await Task.Delay(20, ct).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Scte35DvbService.Instance.Log("INPUT_FAULT", $"Custom Ingest Hub error on '{item.Title}': {ex.Message}", "ERROR");
        }
    }

    private static bool IsSameSource(PlaylistItem a, PlaylistItem b)
    {
        if (string.Equals(a.SourceKind, b.SourceKind, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.VideoDevice, b.VideoDevice, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.FilePath, b.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}
