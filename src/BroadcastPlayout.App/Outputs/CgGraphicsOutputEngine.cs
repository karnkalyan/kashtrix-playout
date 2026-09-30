using System;
using System.Diagnostics;
using System.Threading;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Outputs;

/// <summary>
/// Dedicated real-time CG output path. Renders PROGRAM graphics over a transparent BGRA surface,
/// then publishes the identical fill+alpha frame through NDI and/or a DeckLink hardware keyer.
/// It intentionally runs on a worker thread so synchronous SDI output cannot block WPF operation.
/// </summary>
public sealed class CgGraphicsOutputEngine : IDisposable
{
    private sealed record Config(
        bool NdiEnabled, string NdiName, bool DeckLinkEnabled, int DeckLinkDevice, string DeckLinkDeviceName,
        DeckLinkKeyerMode KeyerMode, byte KeyerLevel, OutputProfile Profile);

    private readonly object _sync = new();
    private readonly AutoResetEvent _wake = new(false);
    private readonly CgCompositor _compositor = new();
    private readonly NdiSender _ndi = new();
    private readonly DeckLinkOutputAdapter _deckLink = new() { EnableAudio = false };
    private readonly Thread _worker;
    private Config _config = new(false, "KASHTRIX-CG-ALPHA", false, 0, "DeckLink", DeckLinkKeyerMode.External, 255, new OutputProfile { Preset = "1920x1080", FramesPerSecond = 50 });
    private CgProject? _program;
    private CgProject? _previousProgram;
    private DateTime _programStartedUtc = DateTime.UtcNow;
    private DateTime _transitionStartedUtc = DateTime.MinValue;
    private string _transitionMode = "None";
    private double _transitionSeconds = 0.45;
    private bool _reconfigure = true;
    private bool _stop;
    private string _lastNdiName = "";
    private string _lastStatus = "CG OUTPUT · OFF";
    private VideoFrameData? _transparentBase;
    private int _prerollRemaining;

    public CgGraphicsOutputEngine()
    {
        _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "Kashtrix CG Alpha / Fill-Key Output" };
        _worker.Start();
    }

    public event Action<string>? StatusChanged;
    public string Status { get { lock (_sync) return _lastStatus; } }

    public void Configure(AppSettings settings)
    {
        var mode = Enum.TryParse<DeckLinkKeyerMode>(settings.CgDeckLinkKeyerMode, true, out var parsed) ? parsed : DeckLinkKeyerMode.External;
        var profile = (settings.CgOutputProfile ?? new OutputProfile { Preset = "1920x1080", FramesPerSecond = 50 }).Clone();
        lock (_sync)
        {
            _config = new Config(
                settings.EnableCgNdiAlphaOutput,
                string.IsNullOrWhiteSpace(settings.CgNdiAlphaSourceName) ? "KASHTRIX-CG-ALPHA" : settings.CgNdiAlphaSourceName.Trim(),
                settings.EnableCgDeckLinkKeyFill,
                Math.Max(0, settings.CgDeckLinkDeviceIndex),
                string.IsNullOrWhiteSpace(settings.CgDeckLinkDeviceName) ? "DeckLink" : settings.CgDeckLinkDeviceName.Trim(),
                mode,
                (byte)Math.Clamp(settings.CgDeckLinkKeyerLevel, 0, 255),
                profile);
            _reconfigure = true;
            _prerollRemaining = Math.Max(0, profile.PrerollFrames);
        }
        _wake.Set();
    }

    public void SetProgram(CgProject project, string transition = "None", double transitionSeconds = 0.45)
    {
        lock (_sync)
        {
            var mode = string.IsNullOrWhiteSpace(transition) ? "None" : transition.Trim();
            _previousProgram = !mode.Equals("None", StringComparison.OrdinalIgnoreCase) ? _program : null;
            _program = project;
            _programStartedUtc = DateTime.UtcNow;
            _transitionMode = mode;
            _transitionSeconds = Math.Clamp(transitionSeconds, .05, 5.0);
            _transitionStartedUtc = _previousProgram is null || mode.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? DateTime.MinValue
                : DateTime.UtcNow;
            _prerollRemaining = Math.Max(0, _config.Profile.PrerollFrames);
        }
        _wake.Set();
    }

    public void ClearProgram()
    {
        lock (_sync)
        {
            _program = null;
            _previousProgram = null;
            _transitionMode = "None";
            _transitionStartedUtc = DateTime.MinValue;
        }
        _wake.Set();
    }

    private void WorkerLoop()
    {
        var sw = Stopwatch.StartNew();
        var lastReport = TimeSpan.Zero;
        while (true)
        {
            Config config;
            CgProject? project;
            CgProject? previousProject;
            DateTime started;
            DateTime transitionStarted;
            string transitionMode;
            double transitionSeconds;
            bool reconfigure;
            lock (_sync)
            {
                if (_stop) break;
                config = _config;
                project = _program;
                previousProject = _previousProgram;
                started = _programStartedUtc;
                transitionStarted = _transitionStartedUtc;
                transitionMode = _transitionMode;
                transitionSeconds = _transitionSeconds;
                reconfigure = _reconfigure;
                _reconfigure = false;
            }

            if (reconfigure)
            {
                _ndi.Close();
                _deckLink.Close();
                _lastNdiName = "";
            }

            if (!config.NdiEnabled && !config.DeckLinkEnabled)
            {
                PublishStatus("CG OUTPUT · OFF");
                _wake.WaitOne(250);
                continue;
            }

            try
            {
                var frameStarted = sw.Elapsed;
                var sourceW = Math.Max(16, project?.Width ?? 1920);
                var sourceH = Math.Max(16, project?.Height ?? 1080);
                var (width, height) = config.Profile.Resolve(sourceW, sourceH);
                width = Math.Max(16, width); height = Math.Max(16, height);
                var fps = Math.Clamp(config.Profile.FramesPerSecond, 1, 120);
                var (fpsN, fpsD) = FpsFraction(fps);
                if (_transparentBase is null || _transparentBase.Width != width || _transparentBase.Height != height || _transparentBase.FrameRateNumerator != fpsN || _transparentBase.FrameRateDenominator != fpsD)
                    _transparentBase = new VideoFrameData(new byte[checked(width * height * 4)], width, height, width * 4, 0, fpsN, fpsD);
                var prerolling = _prerollRemaining > 0;
                if (project is not null && !prerolling) project.OnAir = true;
                var now = DateTime.UtcNow;
                var timeline = project is null || prerolling ? 0 : Math.Max(0, (now - started).TotalSeconds);
                VideoFrameData frame;
                var transitionElapsed = transitionStarted == DateTime.MinValue ? double.MaxValue : (now - transitionStarted).TotalSeconds;
                if (!prerolling && project is not null && previousProject is not null &&
                    !transitionMode.Equals("None", StringComparison.OrdinalIgnoreCase) && transitionElapsed < transitionSeconds)
                {
                    frame = _compositor.CompositeTransition(
                        _transparentBase,
                        previousProject,
                        project,
                        timeline,
                        timeline,
                        Math.Clamp(transitionElapsed / Math.Max(.05, transitionSeconds), 0, 1),
                        transitionMode,
                        alphaSurface: true);
                }
                else
                {
                    frame = _compositor.Composite(_transparentBase, prerolling ? null : project, timeline, alphaSurface: true);
                    if (previousProject is not null && transitionElapsed >= transitionSeconds)
                    {
                        lock (_sync)
                        {
                            if (ReferenceEquals(_previousProgram, previousProject))
                            {
                                _previousProgram = null;
                                _transitionMode = "None";
                                _transitionStartedUtc = DateTime.MinValue;
                            }
                        }
                    }
                }
                if (config.Profile.AlphaMode.StartsWith("Premultiplied", StringComparison.OrdinalIgnoreCase))
                    frame = PremultiplyAlpha(frame);

                if (config.NdiEnabled)
                {
                    if (!_ndi.IsOpen || !_lastNdiName.Equals(config.NdiName, StringComparison.Ordinal))
                    {
                        _ndi.Close();
                        if (!_ndi.Open(config.NdiName)) throw new InvalidOperationException("NDI alpha: " + _ndi.LastError);
                        _lastNdiName = config.NdiName;
                    }
                    _ndi.SendVideo(frame, config.Profile);
                }
                else if (_ndi.IsOpen)
                {
                    _ndi.Close(); _lastNdiName = "";
                }

                if (config.DeckLinkEnabled)
                {
                    _deckLink.DeviceIndex = config.DeckLinkDevice;
                    _deckLink.KeyerMode = config.KeyerMode;
                    _deckLink.KeyerLevel = config.KeyerLevel;
                    _deckLink.EnableAudio = false;
                    _deckLink.SendVideo(frame, config.Profile);
                    if (!string.IsNullOrWhiteSpace(_deckLink.LastError))
                        throw new InvalidOperationException(_deckLink.LastError);
                }
                else if (_deckLink.IsOpen) _deckLink.Close();

                if (sw.Elapsed - lastReport >= TimeSpan.FromSeconds(1))
                {
                    lastReport = sw.Elapsed;
                    var ndi = config.NdiEnabled ? $"NDI ALPHA ON · {config.NdiName} · RX {_ndi.ConnectionCount}" : "NDI ALPHA OFF";
                    var deck = config.DeckLinkEnabled ? $"DECKLINK {config.KeyerMode.ToString().ToUpperInvariant()} KEY · {config.DeckLinkDeviceName}" : "DECKLINK KEY OFF";
                    var pre = _prerollRemaining > 0 ? $" · PREROLL {_prerollRemaining}" : string.Empty;
                    PublishStatus($"{ndi} · {deck} · {width}x{height}@{fps:0.###} · {config.Profile.ScanMode}{pre}");
                }

                if (_prerollRemaining > 0) _prerollRemaining--;
                var frameBudgetMs = 1000.0 / fps;
                var workMs = (sw.Elapsed - frameStarted).TotalMilliseconds;
                _wake.WaitOne(Math.Max(0, (int)Math.Round(frameBudgetMs - workMs)));
            }
            catch (Exception ex)
            {
                PublishStatus("CG OUTPUT ERROR · " + ex.Message);
                _wake.WaitOne(500);
            }
        }

        try { _ndi.Close(); } catch { }
        try { _deckLink.Close(); } catch { }
    }

    private static VideoFrameData PremultiplyAlpha(VideoFrameData frame)
    {
        var copy = (byte[])frame.Bgra.Clone();
        for (var i = 0; i + 3 < copy.Length; i += 4)
        {
            var a = copy[i + 3];
            copy[i] = (byte)((copy[i] * a + 127) / 255);
            copy[i + 1] = (byte)((copy[i + 1] * a + 127) / 255);
            copy[i + 2] = (byte)((copy[i + 2] * a + 127) / 255);
        }
        return new VideoFrameData(copy, frame.Width, frame.Height, frame.Stride, frame.PtsSeconds, frame.FrameRateNumerator, frame.FrameRateDenominator);
    }

    private static (int N, int D) FpsFraction(double fps)
    {
        if (Math.Abs(fps - 23.976) < .01) return (24000, 1001);
        if (Math.Abs(fps - 29.97) < .01) return (30000, 1001);
        if (Math.Abs(fps - 59.94) < .01) return (60000, 1001);
        return ((int)Math.Round(fps), 1);
    }

    private void PublishStatus(string status)
    {
        lock (_sync)
        {
            if (_lastStatus == status) return;
            _lastStatus = status;
        }
        try { StatusChanged?.Invoke(status); } catch { }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_stop) return;
            _stop = true;
        }
        _wake.Set();
        if (!_worker.Join(TimeSpan.FromSeconds(2))) { /* background thread exits with process */ }
        _ndi.Dispose();
        _deckLink.Dispose();
        _compositor.Dispose();
        _wake.Dispose();
    }
}
