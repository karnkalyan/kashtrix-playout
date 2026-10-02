using System.Runtime.InteropServices;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
#if DECKLINK_SDK
using DeckLinkAPI;
#endif

namespace BroadcastPlayout.Outputs;

public enum DeckLinkKeyerMode
{
    Off,
    Internal,
    External
}

public sealed record DeckLinkDeviceOption(int Index, string ModelName, string DisplayName)
{
    public string PersistentName => string.IsNullOrWhiteSpace(DisplayName) ? ModelName : DisplayName;
    public string Label => string.IsNullOrWhiteSpace(DisplayName) || DisplayName.Equals(ModelName, StringComparison.OrdinalIgnoreCase)
        ? ModelName
        : $"{ModelName} · {DisplayName}";
}

public sealed record DeckLinkDisplayModeOption(string ModeName, int Width, int Height, double FrameRate, string ScanMode)
{
    public string DisplayLabel => $"{ModeName} ({Width}x{Height} @ {FrameRate:0.##} fps {ScanMode})";
    public override string ToString() => DisplayLabel;
}

public sealed class DeckLinkOutputAdapter : IDisposable
{
    private readonly object _sync = new();
    private readonly BgraFrameScaler _scaler = new();
#if DECKLINK_SDK
    private IDeckLink? _device;
    private IDeckLinkOutput? _output;
    private IDeckLinkMutableVideoFrame? _videoFrame;
    private IDeckLinkKeyer? _keyer;
    private int _width, _height;
    private string _openProfileKey = string.Empty;
    public int DeviceIndex { get; set; }
    public DeckLinkKeyerMode KeyerMode { get; set; } = DeckLinkKeyerMode.Off;
    public byte KeyerLevel { get; set; } = 255;
    public bool EnableAudio { get; set; } = true;
    public bool IsSupportedBuild => true;
    public bool IsOpen { get { lock (_sync) return _output is not null; } }
    public string LastError { get; private set; } = "";

    private bool OpenFor(VideoFrameData frame, OutputProfile profile)
    {
        try
        {
            CloseDeckLinkCore();
            IDeckLinkIterator iterator = new CDeckLinkIterator();
            _device = null;
            for (var index = 0; index <= Math.Max(0, DeviceIndex); index++)
            {
                iterator.Next(out var candidate);
                if (candidate is null) break;
                _device = candidate;
            }
            if (_device is null)
                throw new InvalidOperationException("No DeckLink device found. Install Desktop Video and verify the card in Desktop Video Setup.");

            _output = (IDeckLinkOutput)_device;
            _output.GetDisplayModeIterator(out var modes);
            IDeckLinkDisplayMode? chosen = null;
            var compatible = new List<string>();
            while (true)
            {
                modes.Next(out var mode);
                if (mode is null) break;
                if (mode.GetWidth() != frame.Width || mode.GetHeight() != frame.Height) continue;

                var modeFps = GetModeFrameRate(mode);
                var modeScan = GetModeScanMode(mode);
                compatible.Add($"{modeFps:0.###}fps {modeScan}");
                var fpsMatch = modeFps <= 0 || Math.Abs(modeFps - profile.FramesPerSecond) < .08;
                var scanMatch = ScanMatches(profile.ScanMode, modeScan);
                if (fpsMatch && scanMatch) { chosen = mode; break; }
            }

            if (chosen is null)
            {
                var available = compatible.Count == 0 ? "no modes at this raster" : string.Join(", ", compatible.Distinct());
                throw new InvalidOperationException($"DeckLink does not expose {frame.Width}x{frame.Height} @ {profile.FramesPerSecond:0.###} fps · {profile.ScanMode}. Available: {available}.");
            }

            _width = chosen.GetWidth();
            _height = chosen.GetHeight();
            _output.EnableVideoOutput(chosen.GetDisplayMode(), _BMDVideoOutputFlags.bmdVideoOutputFlagDefault);
            _output.CreateVideoFrame(_width, _height, _width * 4, _BMDPixelFormat.bmdFormat8BitBGRA,
                _BMDFrameFlags.bmdFrameFlagDefault, out _videoFrame);
            if (EnableAudio)
                _output.EnableAudioOutput(_BMDAudioSampleRate.bmdAudioSampleRate48kHz,
                    _BMDAudioSampleType.bmdAudioSampleType16bitInteger, 2,
                    _BMDAudioOutputStreamType.bmdAudioOutputStreamContinuous);
            ConfigureKeyerCore();
            _openProfileKey = ProfileKey(profile);
            LastError = "";
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            CloseDeckLinkCore();
            return false;
        }
    }

    private static double GetModeFrameRate(IDeckLinkDisplayMode mode)
    {
        try
        {
            var method = typeof(IDeckLinkDisplayMode).GetMethod("GetFrameRate");
            if (method is null) return 0;
            object?[] args = [0L, 0L];
            method.Invoke(mode, args);
            var duration = Convert.ToInt64(args[0]);
            var scale = Convert.ToInt64(args[1]);
            return duration > 0 && scale > 0 ? scale / (double)duration : 0;
        }
        catch { return 0; }
    }

    private static string GetModeScanMode(IDeckLinkDisplayMode mode)
    {
        try
        {
            var method = typeof(IDeckLinkDisplayMode).GetMethod("GetFieldDominance");
            var raw = method?.Invoke(mode, null)?.ToString() ?? string.Empty;
            if (raw.Contains("Lower", StringComparison.OrdinalIgnoreCase)) return "Interlaced Lower First";
            if (raw.Contains("Upper", StringComparison.OrdinalIgnoreCase)) return "Interlaced Upper First";
            if (raw.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) return "Progressive";
        }
        catch { }
        return "Unknown";
    }

    private static bool ScanMatches(string requested, string actual)
    {
        if (actual.Equals("Unknown", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(requested)) requested = "Progressive";
        return requested.Equals(actual, StringComparison.OrdinalIgnoreCase);
    }

    private static string ProfileKey(OutputProfile profile) => $"{profile.Preset}|{profile.Width}x{profile.Height}|{profile.FramesPerSecond:0.###}|{profile.ScanMode}|{profile.AlphaMode}";


    private void ConfigureKeyerCore()
    {
        _keyer = _device as IDeckLinkKeyer;
        if (KeyerMode == DeckLinkKeyerMode.Off)
        {
            try { _keyer?.Disable(); } catch { }
            return;
        }
        if (_keyer is null)
            throw new NotSupportedException($"DeckLink device {DeviceIndex + 1} does not expose IDeckLinkKeyer. Select a DeckLink model/port that supports {KeyerMode.ToString().ToLowerInvariant()} keying.");

        // tlbimp has emitted both BOOL-as-bool and BOOL-as-int signatures across SDK/toolchain
        // generations. Reflect the generated interop signature so FIX46 remains source-compatible.
        var enable = typeof(IDeckLinkKeyer).GetMethod("Enable") ?? throw new MissingMethodException("IDeckLinkKeyer.Enable");
        var parameters = enable.GetParameters();
        if (parameters.Length != 1)
            throw new MissingMethodException("IDeckLinkKeyer.Enable(bool/int isExternal) signature was not found in the generated DeckLink interop.");

        // Blackmagic keyer API uses one isExternal flag: false = internal key, true = external key.
        // Keep both modes explicit so configuration, diagnostics and build verification cannot
        // accidentally collapse Internal into a generic fallback branch.
        var externalKey = KeyerMode switch
        {
            DeckLinkKeyerMode.External => true,
            DeckLinkKeyerMode.Internal => false,
            _ => throw new InvalidOperationException("DeckLink keyer Enable called while keyer mode is Off.")
        };
        var parameterType = parameters[0].ParameterType;
        object external = parameterType == typeof(bool) ? externalKey : Convert.ChangeType(externalKey ? 1 : 0, parameterType);
        enable.Invoke(_keyer, [external]);
        var setLevel = typeof(IDeckLinkKeyer).GetMethod("SetLevel") ?? throw new MissingMethodException("IDeckLinkKeyer.SetLevel");
        var levelType = setLevel.GetParameters()[0].ParameterType;
        object level = levelType == typeof(byte) ? KeyerLevel : Convert.ChangeType(KeyerLevel, levelType);
        setLevel.Invoke(_keyer, [level]);
    }

    public void SendVideo(VideoFrameData frame, OutputProfile profile)
    {
        lock (_sync)
        {
            try
            {
                var (targetW, targetH) = profile.Resolve(frame.Width, frame.Height);
                var outgoing = _scaler.Scale(frame, targetW, targetH);
                if (_output is null || _videoFrame is null || outgoing.Width != _width || outgoing.Height != _height || !string.Equals(_openProfileKey, ProfileKey(profile), StringComparison.Ordinal))
                {
                    if (!OpenFor(outgoing, profile)) return;
                }

                _videoFrame!.GetBytes(out var ptr);
                var rowBytes = checked(_width * 4);
                if (outgoing.Stride < rowBytes || outgoing.Bgra.Length < outgoing.Stride * _height)
                    throw new InvalidOperationException("DeckLink source BGRA buffer/stride is invalid.");
                for (var y = 0; y < _height; y++)
                    Marshal.Copy(outgoing.Bgra, y * outgoing.Stride, IntPtr.Add(ptr, y * rowBytes), rowBytes);
                _output!.DisplayVideoFrameSync(_videoFrame);
                LastError = "";
            }
            catch (Exception ex)
            {
                LastError = "DeckLink video: " + ex.Message;
                CloseDeckLinkCore();
            }
        }
    }

    public unsafe void SendAudio(AudioChunk chunk)
    {
        lock (_sync)
        {
            if (_output is null || chunk.Pcm16Stereo48k.Length == 0 || chunk.Channels <= 0) return;
            try
            {
                fixed (byte* p = chunk.Pcm16Stereo48k)
                {
                    var sampleFrames = (uint)(chunk.Pcm16Stereo48k.Length / (chunk.Channels * 2));
                    _output.WriteAudioSamplesSync((IntPtr)p, sampleFrames, out _);
                }
            }
            catch (Exception ex)
            {
                LastError = "DeckLink audio: " + ex.Message;
                CloseDeckLinkCore();
            }
        }
    }

    private void CloseDeckLinkCore()
    {
        try { _keyer?.Disable(); } catch { }
        _keyer = null;
        try { _output?.DisableAudioOutput(); } catch { }
        try { _output?.DisableVideoOutput(); } catch { }
        _videoFrame = null;
        _output = null;
        _device = null;
        _width = _height = 0;
        _openProfileKey = string.Empty;
    }
#else
    public int DeviceIndex { get; set; }
    public DeckLinkKeyerMode KeyerMode { get; set; } = DeckLinkKeyerMode.Off;
    public byte KeyerLevel { get; set; } = 255;
    public bool EnableAudio { get; set; } = true;
    public bool IsSupportedBuild => false;
    public bool IsOpen => false;
    public string LastError { get; private set; } = "DeckLink SDK interop is not generated. Run tools\\Build-DeckLinkInterop.ps1 with Blackmagic Desktop Video SDK 16.0.";
    public void SendVideo(VideoFrameData frame, OutputProfile profile) { }
    public void SendAudio(AudioChunk chunk) { }
#endif

    public static IReadOnlyList<DeckLinkDeviceOption> EnumerateDevices()
    {
#if DECKLINK_SDK
        var devices = new List<DeckLinkDeviceOption>();
        try
        {
            IDeckLinkIterator iterator = new CDeckLinkIterator();
            var index = 0;
            while (true)
            {
                iterator.Next(out var device);
                if (device is null) break;
                string model;
                string display;
                try { device.GetModelName(out model); } catch { model = $"DeckLink Device {index + 1}"; }
                try { device.GetDisplayName(out display); } catch { display = model; }
                devices.Add(new DeckLinkDeviceOption(index++, model, display));
            }
        }
        catch { }
        if (devices.Count > 0) return devices;
#endif
        return [new DeckLinkDeviceOption(0, "DeckLink hardware", "Desktop Video SDK scan unavailable")];
    }

    public static IReadOnlyList<DeckLinkDisplayModeOption> EnumerateDisplayModes(int deviceIndex = 0)
    {
#if DECKLINK_SDK
        var modes = new List<DeckLinkDisplayModeOption>();
        try
        {
            IDeckLinkIterator iterator = new CDeckLinkIterator();
            IDeckLink? targetDevice = null;
            for (var index = 0; index <= Math.Max(0, deviceIndex); index++)
            {
                iterator.Next(out var candidate);
                if (candidate is null) break;
                targetDevice = candidate;
            }
            if (targetDevice is IDeckLinkOutput output)
            {
                output.GetDisplayModeIterator(out var modeIterator);
                while (true)
                {
                    modeIterator.Next(out var mode);
                    if (mode is null) break;
                    mode.GetName(out var modeName);
                    var width = mode.GetWidth();
                    var height = mode.GetHeight();
                    var fps = GetModeFrameRate(mode);
                    var scan = GetModeScanMode(mode);
                    modes.Add(new DeckLinkDisplayModeOption(modeName, width, height, fps, scan));
                }
            }
        }
        catch { }
        if (modes.Count > 0) return modes;
#endif
        return
        [
            new DeckLinkDisplayModeOption("1080p50", 1920, 1080, 50.0, "Progressive"),
            new DeckLinkDisplayModeOption("1080i50", 1920, 1080, 25.0, "Interlaced Upper First"),
            new DeckLinkDisplayModeOption("1080p59.94", 1920, 1080, 59.94, "Progressive"),
            new DeckLinkDisplayModeOption("1080i59.94", 1920, 1080, 29.97, "Interlaced Upper First"),
            new DeckLinkDisplayModeOption("1080p60", 1920, 1080, 60.0, "Progressive"),
            new DeckLinkDisplayModeOption("1080p25", 1920, 1080, 25.0, "Progressive"),
            new DeckLinkDisplayModeOption("1080p29.97", 1920, 1080, 29.97, "Progressive"),
            new DeckLinkDisplayModeOption("1080p24", 1920, 1080, 24.0, "Progressive"),
            new DeckLinkDisplayModeOption("1080p23.98", 1920, 1080, 23.976, "Progressive"),
            new DeckLinkDisplayModeOption("720p50", 1280, 720, 50.0, "Progressive"),
            new DeckLinkDisplayModeOption("720p59.94", 1280, 720, 59.94, "Progressive"),
            new DeckLinkDisplayModeOption("720p60", 1280, 720, 60.0, "Progressive"),
            new DeckLinkDisplayModeOption("2160p50", 3840, 2160, 50.0, "Progressive"),
            new DeckLinkDisplayModeOption("2160p59.94", 3840, 2160, 59.94, "Progressive"),
            new DeckLinkDisplayModeOption("2160p60", 3840, 2160, 60.0, "Progressive"),
            new DeckLinkDisplayModeOption("2160p25", 3840, 2160, 25.0, "Progressive"),
            new DeckLinkDisplayModeOption("2160p29.97", 3840, 2160, 29.97, "Progressive")
        ];
    }

    public void Close()
    {
#if DECKLINK_SDK
        lock (_sync) CloseDeckLinkCore();
#endif
    }

    public void Dispose()
    {
        Close();
        _scaler.Dispose();
    }
}
