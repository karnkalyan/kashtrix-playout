using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Text.RegularExpressions;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
using BroadcastPlayout.Outputs;
using BroadcastPlayout.Services;
using Kashtrix.OutputEngine.Models;

namespace Kashtrix.OutputEngine.Services;

internal sealed partial class OutputChannelRuntime : IDisposable
{
    private readonly OutputChannel _channel;
    private readonly Action<OutputRuntimeUpdate> _update;
    private readonly CancellationTokenSource _cts = new();
    private readonly OutputProfile _profile;
    private Task? _worker;
    private NdiSender? _ndi;
    private DeckLinkOutputAdapter? _deckLink;
    private VirtualOutputBridge? _virtual;
    private NetworkFfmpegSink? _network;
    private bool _disposed;
    private long _pendingFrames;
    private DateTime _lastTelemetryUtc = DateTime.MinValue;

    public OutputChannelRuntime(OutputChannel channel, Action<OutputRuntimeUpdate> update)
    {
        _channel = channel;
        _update = update;
        _profile = BuildProfile(channel);
    }

    public void Start()
    {
        if (_worker is not null) return;
        switch (_channel.Protocol)
        {
            case BroadcastOutputProtocol.NDI:
            case BroadcastOutputProtocol.CgOutput:
                _ndi = new NdiSender();
                var sourceName = string.IsNullOrWhiteSpace(_channel.DestinationUri) ? _channel.Name : _channel.DestinationUri.Trim();
                if (!_ndi.Open(sourceName)) throw new InvalidOperationException(_ndi.LastError);
                break;
            case BroadcastOutputProtocol.DeckLink:
                _deckLink = new DeckLinkOutputAdapter { DeviceIndex = ResolveDeckLinkDeviceIndex(_channel.DestinationUri) };
                if (!_deckLink.IsSupportedBuild) throw new InvalidOperationException(_deckLink.LastError);
                break;
            case BroadcastOutputProtocol.VirtualOutput:
                _virtual = new VirtualOutputBridge();
                break;
            case BroadcastOutputProtocol.Matrox:
            case BroadcastOutputProtocol.AJA:
                throw new NotSupportedException($"{_channel.ProtocolDisplayName} runtime is not installed. Install the vendor SDK/output plug-in before starting this channel.");
            default:
                _network = new NetworkFfmpegSink(_channel, _profile);
                break;
        }

        _worker = Task.Run(() => RunAsync(_cts.Token));
    }

    private static readonly Lazy<VirtualOutputBridge> _customPreviewBridge = new(() => new VirtualOutputBridge(VirtualOutputBridge.CustomPreviewVideoMapName, VirtualOutputBridge.CustomPreviewAudioMapName));

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            _update(new OutputRuntimeUpdate(OutputStatus.Starting, "Waiting for source signal", 0, 0, string.Empty));
            if (_channel.InputSource == OutputInputSource.Manual)
            {
                var item = BuildManualPlaylistItem(_channel);
                CustomInputHub.Instance.EnsureRunning(item);
            }
            await RunSharedBusAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!ct.IsCancellationRequested)
            {
                var errorMsg = ex.InnerException?.Message ?? ex.Message;
                _update(new OutputRuntimeUpdate(OutputStatus.Error, errorMsg, 0, 0, errorMsg));
                Scte35DvbService.Instance.Log("OUTPUT_FAULT", $"[{_channel.Name}] I/O failure: {errorMsg}", "ERROR", _channel.Name);
            }
        }
    }

    private async Task RunSharedBusAsync(CancellationToken ct)
    {
        var mapName = _channel.InputSource switch
        {
            OutputInputSource.CgProgram => VirtualOutputBridge.CgProgramVideoMapName,
            OutputInputSource.Manual => VirtualOutputBridge.CustomPreviewVideoMapName,
            _ => VirtualOutputBridge.ProgramConfidenceVideoMapName
        };
        using var reader = new SharedVideoBusReader(mapName);
        VideoFrameData? latest = null;
        var signalDeadline = DateTime.UtcNow.AddSeconds(3);
        var interval = TimeSpan.FromSeconds(1.0 / Math.Clamp(_channel.TargetFps, 1, 120));
        while (!ct.IsCancellationRequested)
        {
            if (reader.TryRead(out var frame)) latest = frame;
            if (latest is null)
            {
                if (DateTime.UtcNow >= signalDeadline)
                {
                    _update(new OutputRuntimeUpdate(OutputStatus.Warning, $"No frames on {_channel.InputSourceDisplayName}", 0, 0, $"No source signal on {mapName}. Start Playout/CG Program or verify custom input."));
                    signalDeadline = DateTime.UtcNow.AddSeconds(3);
                }
            }
            else
            {
                SendFrame(latest);
            }
            await Task.Delay(interval, ct).ConfigureAwait(false);
        }
    }

    private void SendFrame(VideoFrameData frame)
    {

        if (_ndi is not null)
        {
            _ndi.SendVideo(frame, _profile);
            if (!string.IsNullOrWhiteSpace(_ndi.LastError)) throw new InvalidOperationException(_ndi.LastError);
        }
        else if (_deckLink is not null)
        {
            _deckLink.SendVideo(frame, _profile);
            if (!string.IsNullOrWhiteSpace(_deckLink.LastError)) throw new InvalidOperationException(_deckLink.LastError);
        }
        else if (_virtual is not null)
        {
            if (!_virtual.IsOpen)
            {
                var (w, h) = _profile.Resolve(frame.Width, frame.Height);
                _virtual.Open(w, h, _profile.FramesPerSecond);
            }
            _virtual.SubmitVideo(frame, onAir: true);
        }
        else
        {
            _network!.Send(frame);
        }

        _pendingFrames++;
        if (DateTime.UtcNow - _lastTelemetryUtc >= TimeSpan.FromMilliseconds(250))
        {
            var connections = _ndi?.ConnectionCount ?? -1;
            var detail = connections >= 0 ? $"NDI connections: {connections}" : "Source and output active";
            var count = _pendingFrames;
            _pendingFrames = 0;
            _lastTelemetryUtc = DateTime.UtcNow;
            _update(new OutputRuntimeUpdate(OutputStatus.Online, detail, count, frame.FramesPerSecond, string.Empty));
        }
    }

    private static PlaylistItem BuildManualPlaylistItem(OutputChannel channel) => new()
    {
        Title = string.IsNullOrWhiteSpace(channel.ManualInputSource) ? channel.Name : channel.ManualInputSource,
        SourceKind = string.IsNullOrWhiteSpace(channel.ManualInputKind) ? "Custom" : channel.ManualInputKind,
        FilePath = channel.ManualInputSource,
        InputFormat = channel.ManualInputFormat,
        InputOptions = channel.ManualInputOptions,
        VideoDevice = channel.ManualVideoDevice,
        AudioDevice = channel.ManualAudioDevice,
        AlternateAudioUrl = channel.ManualAlternateAudioUrl,
        IsLiveSource = channel.ManualIsLiveSource,
        CaptureWidth = channel.ManualCaptureWidth,
        CaptureHeight = channel.ManualCaptureHeight,
        SourceFrameRate = channel.ManualSourceFrameRate > 0
            ? channel.ManualSourceFrameRate
            : (string.Equals(channel.ManualInputKind, "DirectShow", StringComparison.OrdinalIgnoreCase) ? 0 : channel.TargetFps),
        SourceDuration = TimeSpan.FromHours(24),
        OutPoint = TimeSpan.FromHours(24)
    };

    private static OutputProfile BuildProfile(OutputChannel channel)
    {
        var profile = new OutputProfile { FramesPerSecond = Math.Clamp(channel.TargetFps, 1, 120) };
        if (channel.IsCustomResolution)
        {
            profile.Preset = "Custom";
            profile.Width = channel.CustomWidth;
            profile.Height = channel.CustomHeight;
        }
        else
        {
            var match = DimensionRegex().Match(channel.RasterFormat ?? string.Empty);
            if (match.Success && int.TryParse(match.Groups["w"].Value, out var width) && int.TryParse(match.Groups["h"].Value, out var height))
            {
                profile.Preset = "Custom";
                profile.Width = width;
                profile.Height = height;
            }
            else profile.Preset = "Source";
        }
        profile.ScanMode = (channel.RasterFormat ?? string.Empty).Contains('i') ? "Interlaced Upper First" : "Progressive";
        return profile;
    }

    private static int ResolveDeckLinkDeviceIndex(string? destination)
    {
        var devices = DeckLinkOutputAdapter.EnumerateDevices();
        if (devices.Count == 0) return 0;
        var text = destination ?? string.Empty;
        var exact = devices.FirstOrDefault(x => text.Contains(x.PersistentName, StringComparison.OrdinalIgnoreCase) || text.Contains(x.ModelName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.Index;
        var number = Regex.Match(text, @"(?:device|output|port)\s*(\d+)", RegexOptions.IgnoreCase);
        return number.Success && int.TryParse(number.Groups[1].Value, out var oneBased) ? Math.Clamp(oneBased - 1, 0, devices.Count - 1) : 0;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        try { _worker?.Wait(1200); } catch { }
        _network?.Dispose();
        _virtual?.Dispose();
        _deckLink?.Dispose();
        _ndi?.Dispose();
        _cts.Dispose();
    }

    [GeneratedRegex(@"(?<w>\d{3,5})\s*[xX]\s*(?<h>\d{3,5})")]
    private static partial Regex DimensionRegex();
}

internal sealed record OutputRuntimeUpdate(OutputStatus Status, string Detail, long FramesSent, double Fps, string Error);

internal sealed class SharedVideoBusReader : IDisposable
{
    private readonly string _mapName;
    private MemoryMappedFile? _map;
    private MemoryMappedViewAccessor? _view;
    private long _sequence;

    public SharedVideoBusReader(string mapName) => _mapName = mapName;

    public bool TryRead(out VideoFrameData frame)
    {
        frame = null!;
        try
        {
            if (_view is null)
            {
                _map = MemoryMappedFile.OpenExisting(_mapName, MemoryMappedFileRights.Read);
                _view = _map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            }
            if (_view.ReadInt32(0) != 0x4B545856) return false;
            var width = _view.ReadInt32(8);
            var height = _view.ReadInt32(12);
            var stride = _view.ReadInt32(16);
            var fps = _view.ReadDouble(20);
            var sequence = _view.ReadInt64(28);
            var length = _view.ReadInt32(44);
            if (sequence == _sequence || width <= 0 || height <= 0 || stride < width * 4 || length <= 0 || length > stride * height) return false;
            var pixels = new byte[length];
            _view.ReadArray(256, pixels, 0, pixels.Length);
            if (_view.ReadInt64(28) != sequence) return false;
            var denominator = 1000;
            var numerator = Math.Max(1, (int)Math.Round(Math.Clamp(fps, 1, 120) * denominator));
            frame = new VideoFrameData(pixels, width, height, stride, 0, numerator, denominator);
            _sequence = sequence;
            return true;
        }
        catch
        {
            _view?.Dispose(); _view = null;
            _map?.Dispose(); _map = null;
            return false;
        }
    }

    public void Dispose() { _view?.Dispose(); _map?.Dispose(); }
}

internal sealed class NetworkFfmpegSink : IDisposable
{
    private readonly OutputChannel _channel;
    private readonly OutputProfile _profile;
    private readonly BgraFrameScaler _scaler = new();
    private Process? _process;
    private int _width;
    private int _height;
    private bool _forceSoftwareEncoder;
    private string _lastProcessError = string.Empty;

    public NetworkFfmpegSink(OutputChannel channel, OutputProfile profile)
    {
        _channel = channel;
        _profile = profile;
    }

    public void Send(VideoFrameData frame)
    {
        var (targetWidth, targetHeight) = _profile.Resolve(frame.Width, frame.Height);
        var outgoing = _scaler.Scale(frame, targetWidth, targetHeight);
        try
        {
            WriteFrame(outgoing);
        }
        catch (Exception) when (!_forceSoftwareEncoder && ResolveEncoder(_channel.GpuEncoder) != "libx264")
        {
            // Hardware encoders can be compiled into FFmpeg even when the matching GPU/driver
            // is unavailable. Keep the output on air by retrying once with the software encoder.
            _forceSoftwareEncoder = true;
            DisposeProcess();
            WriteFrame(outgoing);
        }
    }

    private void WriteFrame(VideoFrameData frame)
    {
        EnsureProcess(frame.Width, frame.Height);
        if (_process is null || _process.HasExited)
        {
            var detail = string.IsNullOrWhiteSpace(_lastProcessError) ? string.Empty : $" {_lastProcessError}";
            throw new InvalidOperationException($"FFmpeg output process stopped before accepting video frames.{detail}");
        }
        _process.StandardInput.BaseStream.Write(frame.Bgra, 0, frame.Bgra.Length);
        _process.StandardInput.BaseStream.Flush();
    }

    private void EnsureProcess(int width, int height)
    {
        if (_process is { HasExited: false } && _width == width && _height == height) return;
        DisposeProcess();
        var ffmpeg = InputDeviceService.FindFfmpegExecutable() ?? throw new FileNotFoundException("Bundled FFmpeg executable was not found.");
        var psi = new ProcessStartInfo { FileName = ffmpeg, UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in BuildArguments(width, height)) psi.ArgumentList.Add(arg);
        var startedProcess = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start FFmpeg output process.");
        _process = startedProcess;
        _width = width; _height = height;
        _lastProcessError = string.Empty;
        _ = Task.Run(async () =>
        {
            try { _lastProcessError = (await startedProcess.StandardError.ReadToEndAsync().ConfigureAwait(false)).Trim(); }
            catch { }
        });
    }

    private IEnumerable<string> BuildArguments(int width, int height)
    {
        var fps = Math.Clamp(_profile.FramesPerSecond, 1, 120).ToString("0.###", CultureInfo.InvariantCulture);
        var bitrate = Math.Max(.1, _channel.BitrateMbps).ToString("0.###", CultureInfo.InvariantCulture) + "M";
        var encoder = _forceSoftwareEncoder ? "libx264" : ResolveEncoder(_channel.GpuEncoder);
        var common = new List<string> { "-hide_banner", "-loglevel", "warning", "-f", "rawvideo", "-pix_fmt", "bgra", "-video_size", $"{width}x{height}", "-framerate", fps, "-i", "-", "-an", "-c:v", encoder, "-b:v", bitrate, "-maxrate", bitrate, "-bufsize", bitrate, "-pix_fmt", "yuv420p" };
        if (encoder == "libx264") common.AddRange(["-preset", "veryfast", "-tune", "zerolatency"]);
        switch (_channel.Protocol)
        {
            case BroadcastOutputProtocol.DvbUdp:
                common.AddRange(["-f", "mpegts", "-mpegts_service_id", _channel.ServiceId.ToString(CultureInfo.InvariantCulture), _channel.DestinationUri]);
                break;
            case BroadcastOutputProtocol.SRT:
                common.AddRange(["-f", "mpegts", _channel.DestinationUri]);
                break;
            case BroadcastOutputProtocol.RTMP:
                common.AddRange(["-f", "flv", _channel.DestinationUri]);
                break;
            case BroadcastOutputProtocol.HLS:
                common.AddRange(["-f", "hls", "-hls_time", "2", "-hls_list_size", "6", _channel.DestinationUri]);
                break;
            case BroadcastOutputProtocol.MPD:
                common.AddRange(["-f", "dash", "-seg_duration", "2", _channel.DestinationUri]);
                break;
            case BroadcastOutputProtocol.RTSP:
                common.AddRange(["-f", "rtsp", "-rtsp_transport", "tcp", _channel.DestinationUri]);
                break;
        }
        return common;
    }

    private static string ResolveEncoder(string requested)
    {
        if (requested.Contains("QSV", StringComparison.OrdinalIgnoreCase)) return "h264_qsv";
        if (requested.Contains("AMF", StringComparison.OrdinalIgnoreCase)) return "h264_amf";
        if (requested.Contains("NVENC", StringComparison.OrdinalIgnoreCase) || requested.Contains("CUDA", StringComparison.OrdinalIgnoreCase)) return "h264_nvenc";
        if (requested.Contains("Media Foundation", StringComparison.OrdinalIgnoreCase) || requested.Contains("MF", StringComparison.OrdinalIgnoreCase)) return "h264_mf";
        return "libx264";
    }

    private void DisposeProcess()
    {
        var process = _process; _process = null;
        if (process is null) return;
        try { process.StandardInput.Close(); } catch { }
        try { if (!process.WaitForExit(800)) process.Kill(entireProcessTree: true); } catch { }
        process.Dispose();
    }

    public void Dispose() { DisposeProcess(); _scaler.Dispose(); }
}
