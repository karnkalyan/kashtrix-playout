using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public partial class InputSourceWindow : Window
{
    private MediaInfo? _urlProbe;
    private string? _resolvedWebAudioUrl;
    private string? _resolvedWebVideoUrl;
    private string? _originalWebPageUrl;
    private bool _isResolvingWebUrl;
    private CancellationTokenSource? _previewCts;
    private CancellationTokenSource? _ndiScanCts;
    private readonly DispatcherTimer _ndiScanTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private long _previewGeneration;
    private volatile bool _isPreviewPaused;
    private TimeSpan _currentPreviewPosition = TimeSpan.Zero;
    private TimeSpan _currentPreviewInPoint = TimeSpan.Zero;
    private TimeSpan _currentPreviewDuration = TimeSpan.Zero;
    private volatile bool _isUserSeeking;
    private VideoDecoder? _activePreviewDecoder;
    public PlaylistItem? ResultItem { get; private set; }

    public InputSourceWindow()
    {
        InitializeComponent();
        _ndiScanTimer.Tick += async (_, _) => await ScanNdiSourcesAsync(false);
        Loaded += async (_, _) =>
        {
            await RefreshDevicesAsync();
            await ScanNdiSourcesAsync(false);
            _ndiScanTimer.Start();
            QueuePreviewRestart();
        };
        Closed += (_, _) =>
        {
            _ndiScanTimer.Stop();
            try { _ndiScanCts?.Cancel(); _ndiScanCts?.Dispose(); } catch { }
            StopPreview();
        };
    }

    /// <summary>Pre-fills the window from an existing source so it can be edited (used by Input Engine double-click).</summary>
    public void LoadItem(PlaylistItem item)
    {
        TitleBox.Text = item.Title;
        var kind = item.SourceKind ?? string.Empty;
        if (kind.Equals("URL", StringComparison.OrdinalIgnoreCase))
        {
            SourceTabs.SelectedIndex = 0;
            UrlBox.Text = item.FilePath;
            UrlFormatBox.Text = item.InputFormat ?? string.Empty;
            UrlLiveCheck.IsChecked = item.IsLiveSource;
        }
        else if (kind.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            SourceTabs.SelectedIndex = 4;
            CustomSourceBox.Text = item.FilePath;
            CustomFormatBox.Text = item.InputFormat ?? string.Empty;
            CustomOptionsBox.Text = item.InputOptions ?? string.Empty;
            CustomLiveCheck.IsChecked = item.IsLiveSource;
        }
    }

    private async void RefreshDevices_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        DeviceStatus.Text = "Scanning Windows capture devices...";
        try
        {
            var devices = await InputDeviceService.EnumerateDirectShowAsync();
            VideoDeviceBox.ItemsSource = devices.VideoDevices;
            AudioDeviceBox.ItemsSource = devices.AudioDevices;
            if (devices.VideoDevices.Count > 0 && VideoDeviceBox.SelectedIndex < 0) VideoDeviceBox.SelectedIndex = 0;
            if (devices.AudioDevices.Count > 0 && AudioDeviceBox.SelectedIndex < 0) AudioDeviceBox.SelectedIndex = 0;
            var selectedVideo = GetComboText(VideoDeviceBox);
            DeviceStatus.Text = $"Found {devices.VideoDevices.Count} video and {devices.AudioDevices.Count} audio device(s)." +
                                (string.IsNullOrWhiteSpace(selectedVideo) ? "" : $" Selected: {selectedVideo}");
        }
        catch (Exception ex)
        {
            DeviceStatus.Text = "Device scan failed: " + Friendly(ex);
        }
    }

    private async void ScanNdi_Click(object sender, RoutedEventArgs e) => await ScanNdiSourcesAsync(true);

    private async Task ScanNdiSourcesAsync(bool userInitiated)
    {
        if (NdiSourceBox is null) return;
        var old = _ndiScanCts;
        _ndiScanCts = new CancellationTokenSource();
        try { old?.Cancel(); old?.Dispose(); } catch { }
        var cts = _ndiScanCts;
        if (userInitiated) NdiStatusText.Text = "Scanning NDI network for 10 seconds…";
        try
        {
            var selectedName = (NdiSourceBox.SelectedItem as NdiDiscoveryService.NdiSourceInfo)?.Name;
            var sources = await NdiDiscoveryService.ScanAsync(TimeSpan.FromSeconds(10), cts.Token);
            if (cts.IsCancellationRequested) return;
            NdiSourceBox.ItemsSource = sources;
            if (!string.IsNullOrWhiteSpace(selectedName))
                NdiSourceBox.SelectedItem = sources.FirstOrDefault(x => x.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
            if (NdiSourceBox.SelectedIndex < 0 && sources.Count > 0) NdiSourceBox.SelectedIndex = 0;
            NdiStatusText.Text = sources.Count == 0
                ? "No NDI sources found in the 10-second discovery window."
                : $"Found {sources.Count} NDI source(s). Discovery refreshes every 10 seconds.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { NdiStatusText.Text = "NDI scan failed: " + Friendly(ex); }
    }

    private void NdiSource_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (NdiSourceBox.SelectedItem is NdiDiscoveryService.NdiSourceInfo source)
        {
            NdiStatusText.Text = $"Selected: {source.Name}" + (string.IsNullOrWhiteSpace(source.Address) ? string.Empty : $" · {source.Address}");
            if (string.IsNullOrWhiteSpace(TitleBox.Text) || TitleBox.Text.Equals("Live Input", StringComparison.OrdinalIgnoreCase)) TitleBox.Text = source.Name;
        }
    }

    private async void ProbeUrl_Click(object sender, RoutedEventArgs e)
    {
        await ResolveWebUrlIfNeededAsync();
        var url = UrlBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(url)) return;
        UrlProbeStatus.Text = "Probing source...";
        try
        {
            var probeItem = new PlaylistItem { SourceKind = "URL", FilePath = url, AlternateAudioUrl = _resolvedWebAudioUrl ?? string.Empty, InputFormat = UrlFormatBox.Text.Trim(), IsLiveSource = false };
            var info = await Task.Run(() => MediaProbe.Read(probeItem));
            _urlProbe = info;
            _currentPreviewDuration = info.Duration;
            PreviewDurationText.Text = Format(info.Duration);
            PreviewSeekSlider.Maximum = Math.Max(1, info.Duration.TotalSeconds);
            UrlDurationBox.Text = Format(info.Duration);
            UrlLiveCheck.IsChecked = false;
            UrlProbeStatus.Text = $"VOD detected · {info.Width}x{info.Height} · {info.FramesPerSecond:0.###} fps · {info.VideoCodec} · {Format(info.Duration)}";
            QueuePreviewRestart();
        }
        catch (Exception ex)
        {
            UrlProbeStatus.Text = "Probe failed: " + Friendly(ex);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SourceTabs.SelectedIndex == 0) await ResolveWebUrlIfNeededAsync();
            ResultItem = CreateSelectedItem();
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(Friendly(ex), "Add Input Source", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ResolveWebUrl_Click(object sender, RoutedEventArgs e)
    {
        try { await ResolveWebUrlIfNeededAsync(force: true); }
        catch (Exception ex) { UrlProbeStatus.Text = "Web resolver failed: " + Friendly(ex); }
    }

    private async Task ResolveWebUrlIfNeededAsync(bool force = false)
    {
        var value = UrlBox.Text.Trim();
        if (!force && !YtDlpResolver.LooksLikeWebVideoPage(value)) return;
        if (!YtDlpResolver.LooksLikeWebVideoPage(value))
        {
            if (force) UrlProbeStatus.Text = "This is already a direct stream/media URL; yt-dlp is not required.";
            return;
        }

        UrlProbeStatus.Text = "Resolving web page with yt-dlp...";
        _isResolvingWebUrl = true;
        try
        {
            var result = await YtDlpResolver.ResolveAsync(value);
            _originalWebPageUrl = value;
            _resolvedWebVideoUrl = result.MediaUrl;
            _resolvedWebAudioUrl = result.AudioUrl;
            if (!string.IsNullOrWhiteSpace(result.Title) &&
                (string.IsNullOrWhiteSpace(TitleBox.Text) ||
                 TitleBox.Text.Equals("URL INPUT", StringComparison.OrdinalIgnoreCase) ||
                 TitleBox.Text.Equals("Live Input", StringComparison.OrdinalIgnoreCase)))
                TitleBox.Text = result.Title;
            UrlLiveCheck.IsChecked = false;
            UrlProbeStatus.Text = result.AudioUrl is null
                ? $"Resolved web video: {result.Title} · direct A/V media URL ready"
                : $"Resolved web video: {result.Title} · separate video + audio streams ready";
            QueuePreviewRestart();
        }
        finally
        {
            _isResolvingWebUrl = false;
        }
    }

    private PlaylistItem CreateSelectedItem() => SourceTabs.SelectedIndex switch
    {
        0 => CreateUrlItem(),
        1 => CreateCaptureItem(),
        2 => CreateNdiItem(),
        3 => CreateScreenItem(),
        4 => CreateCustomItem(),
        5 => CreateStudioItem(),
        _ => throw new InvalidOperationException("Select an input source type.")
    };

    private async void StartPreview_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (SourceTabs.SelectedIndex == 0) await ResolveWebUrlIfNeededAsync();
            await StartPreviewAsync();
        }
        catch (Exception ex) { SetPreviewState("NO SIGNAL", false, Friendly(ex)); }
    }
    private void StopPreview_Click(object sender, RoutedEventArgs e) => StopPreview();
    private void CaptureDevice_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        var video = GetComboText(VideoDeviceBox);
        var audio = GetComboText(AudioDeviceBox);
        DeviceStatus.Text = string.IsNullOrWhiteSpace(video)
            ? "Select a video device or capture card."
            : $"Selected video: {video}" + (string.IsNullOrWhiteSpace(audio) ? "" : $" · audio: {audio}");
        QueuePreviewRestart();
    }
    private void SourceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) QueuePreviewRestart(); }
    private void PreviewField_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) QueuePreviewRestart(); }
    private void UrlBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isResolvingWebUrl) return;
        _resolvedWebAudioUrl = null;
        _resolvedWebVideoUrl = null;
        _originalWebPageUrl = null;
        _urlProbe = null;
    }

    private void QueuePreviewRestart()
    {
        var generation = ++_previewGeneration;
        _ = Task.Run(async () =>
        {
            await Task.Delay(300).ConfigureAwait(false);
            if (generation != Interlocked.Read(ref _previewGeneration) || !IsDispatcherAlive()) return;
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (IsLoaded && generation == Interlocked.Read(ref _previewGeneration))
                        await StartPreviewAsync();
                });
            }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        });
    }

    private async void PreviewPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_previewCts == null || _previewCts.IsCancellationRequested)
        {
            _isPreviewPaused = false;
            await StartPreviewAsync();
        }
        else
        {
            _isPreviewPaused = false;
            SetPreviewState("LIVE PREVIEW", true, "Preview running.");
        }
    }

    private void PreviewPause_Click(object sender, RoutedEventArgs e)
    {
        if (_previewCts != null && !_previewCts.IsCancellationRequested)
        {
            _isPreviewPaused = !_isPreviewPaused;
            if (_isPreviewPaused)
            {
                SetPreviewState("PAUSED", false, "Preview paused.");
            }
            else
            {
                SetPreviewState("LIVE PREVIEW", true, "Preview running.");
            }
        }
    }

    private void PreviewStop_Click(object sender, RoutedEventArgs e)
    {
        _isPreviewPaused = false;
        StopPreview();
    }

    private async Task StartPreviewAsync()
    {
        _isPreviewPaused = false;
        StopPreview();
        PlaylistItem item;
        try { item = CreateSelectedItem(); }
        catch (Exception ex)
        {
            SetPreviewState("WAITING", false, Friendly(ex));
            return;
        }

        var cts = new CancellationTokenSource();
        _previewCts = cts;
        var generation = ++_previewGeneration;
        SetPreviewState("CONNECTING", true, $"Opening {item.Title}...");
        PreviewPlaceholder.Visibility = Visibility.Collapsed;

        _ = Task.Run(() => RunVideoPreview(item, generation, cts.Token));
        _ = Task.Run(() => RunAudioMeters(item, generation, cts.Token));
        await Task.CompletedTask;
    }

    private void RunVideoPreview(PlaylistItem item, long generation, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && generation == Interlocked.Read(ref _previewGeneration))
            {
                using var decoder = new VideoDecoder(item);
                _activePreviewDecoder = decoder;
                var initialSeek = _currentPreviewInPoint > TimeSpan.Zero ? _currentPreviewInPoint : (_currentPreviewPosition > TimeSpan.Zero ? _currentPreviewPosition : TimeSpan.Zero);
                if (!item.IsLiveSource && initialSeek > TimeSpan.Zero)
                {
                    try { decoder.Seek(initialSeek); } catch { }
                }

                var firstPts = double.NaN;
                var clock = System.Diagnostics.Stopwatch.StartNew();
                while (!ct.IsCancellationRequested && generation == Interlocked.Read(ref _previewGeneration))
                {
                    if (!decoder.TryRead(out var frame))
                    {
                        if (item.IsLiveSource)
                        {
                            Thread.Sleep(8);
                            continue;
                        }
                        break;
                    }
                    while (!ct.IsCancellationRequested && _isPreviewPaused && generation == Interlocked.Read(ref _previewGeneration))
                    {
                        Thread.Sleep(30);
                    }
                    if (double.IsNaN(firstPts)) firstPts = frame.PtsSeconds;
                    var target = Math.Max(0, frame.PtsSeconds - firstPts);
                    while (!ct.IsCancellationRequested && clock.Elapsed.TotalSeconds + .002 < target)
                        Thread.Sleep(2);
                    var image = ToBitmapSource(frame);
                    var currentPos = TimeSpan.FromSeconds(Math.Max(0, frame.PtsSeconds));
                    _currentPreviewPosition = currentPos;
                    if (!IsDispatcherAlive()) return;
                    try
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (!IsLoaded || ct.IsCancellationRequested || generation != Interlocked.Read(ref _previewGeneration)) return;
                            InputPreviewImage.Source = image;
                            PreviewFormatText.Text = $"{frame.Width}×{frame.Height}\n{frame.FramesPerSecond:0.###} fps";
                            SetPreviewState("LIVE PREVIEW", true, item.IsLiveSource ? "Preview running continuously." : "Preview running. VOD loops automatically.");
                            if (!_isUserSeeking)
                            {
                                if (_currentPreviewDuration > TimeSpan.Zero)
                                {
                                    PreviewSeekSlider.Maximum = _currentPreviewDuration.TotalSeconds;
                                    PreviewSeekSlider.Value = Math.Min(currentPos.TotalSeconds, PreviewSeekSlider.Maximum);
                                }
                                PreviewPositionText.Text = Format(currentPos);
                            }
                        });
                    }
                    catch (InvalidOperationException) { return; }
                }
                if (item.IsLiveSource) break;
            }
        }
        catch (Exception ex)
        {
            if (!IsDispatcherAlive()) return;
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    if (!IsLoaded || ct.IsCancellationRequested || generation != Interlocked.Read(ref _previewGeneration)) return;
                    SetPreviewState("NO SIGNAL", false, Friendly(ex));
                });
            }
            catch (InvalidOperationException) { }
        }
        finally
        {
            _activePreviewDecoder = null;
        }
    }

    private void MarkIn_Click(object sender, RoutedEventArgs e)
    {
        _currentPreviewInPoint = _currentPreviewPosition;
        PreviewInPointBadge.Text = $"IN: {Format(_currentPreviewInPoint)}";
    }

    private void ResetIn_Click(object sender, RoutedEventArgs e)
    {
        _currentPreviewInPoint = TimeSpan.Zero;
        PreviewInPointBadge.Text = "IN: 00:00:00";
    }

    private void PreviewSeekSlider_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = true;
    }

    private void PreviewSeekSlider_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isUserSeeking = false;
        var target = TimeSpan.FromSeconds(PreviewSeekSlider.Value);
        _currentPreviewPosition = target;
        _currentPreviewInPoint = target;
        PreviewInPointBadge.Text = $"IN: {Format(_currentPreviewInPoint)}";
        PreviewPositionText.Text = Format(target);
        if (_activePreviewDecoder != null)
        {
            try { _activePreviewDecoder.Seek(target); }
            catch { QueuePreviewRestart(); }
        }
        else
        {
            QueuePreviewRestart();
        }
    }

    private void PreviewSeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUserSeeking)
        {
            var pos = TimeSpan.FromSeconds(e.NewValue);
            _currentPreviewPosition = pos;
            _currentPreviewInPoint = pos;
            PreviewInPointBadge.Text = $"IN: {Format(_currentPreviewInPoint)}";
            PreviewPositionText.Text = Format(pos);
        }
    }

    private void RunAudioMeters(PlaylistItem item, long generation, CancellationToken ct)
    {
        if (item.SourceKind.Equals("Screen", StringComparison.OrdinalIgnoreCase)) return;
        if (item.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(item.AudioDevice)) return;
        try
        {
            using var audioOut = new BroadcastPlayout.Outputs.LocalAudioOutput();
            using var decoder = new AudioDecoder(item);
            while (!ct.IsCancellationRequested && generation == Interlocked.Read(ref _previewGeneration))
            {
                if (_isPreviewPaused)
                {
                    Thread.Sleep(30);
                    continue;
                }
                if (!decoder.TryRead(out var chunk))
                {
                    if (item.IsLiveSource)
                    {
                        Thread.Sleep(8);
                        continue;
                    }
                    break;
                }
                audioOut.Send(chunk);
                var (l, r) = Measure(chunk.Pcm16Stereo48k);
                if (!IsDispatcherAlive()) return;
                try
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (!IsLoaded || ct.IsCancellationRequested || generation != Interlocked.Read(ref _previewGeneration)) return;
                        PreviewAudioL.Value = l;
                        PreviewAudioR.Value = r;
                    });
                }
                catch (InvalidOperationException) { return; }
            }
        }
        catch { /* Audio is optional for preview. */ }
    }

    private void StopPreview()
    {
        _isPreviewPaused = false;
        Interlocked.Increment(ref _previewGeneration);
        var old = Interlocked.Exchange(ref _previewCts, null);
        if (old is not null)
        {
            try { old.Cancel(); } catch (ObjectDisposedException) { }
            try { old.Dispose(); } catch { }
        }

        if (!IsDispatcherAlive()) return;
        PreviewAudioL.Value = 0;
        PreviewAudioR.Value = 0;
        SetPreviewState("IDLE", false, "Preview stopped.");
    }

    private bool IsDispatcherAlive() =>
        !Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished;

    private void SetPreviewState(string state, bool good, string detail)
    {
        PreviewStateText.Text = state;
        PreviewStateText.Foreground = good ? (Brush)FindResource("GoodBrush") : (Brush)FindResource("MutedBrush");
        PreviewLed.Fill = good ? (Brush)FindResource("GoodBrush") : new SolidColorBrush(Color.FromRgb(90,101,112));
        PreviewStatusText.Text = detail;
    }

    private static BitmapSource ToBitmapSource(VideoFrameData frame)
    {
        var image = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, frame.Bgra, frame.Stride);
        image.Freeze();
        return image;
    }

    private static (double Left, double Right) Measure(byte[] pcm)
    {
        var l = 0; var r = 0;
        for (var i = 0; i + 3 < pcm.Length; i += 4)
        {
            l = Math.Max(l, Math.Abs((int)BitConverter.ToInt16(pcm, i)));
            r = Math.Max(r, Math.Abs((int)BitConverter.ToInt16(pcm, i + 2)));
        }
        return (Math.Clamp(l / 32768.0, 0, 1), Math.Clamp(r / 32768.0, 0, 1));
    }

    private (TimeSpan InPoint, TimeSpan OutPoint, TimeSpan SourceDuration) CalculatePoints(TimeSpan specifiedDuration, bool isLive)
    {
        var inPoint = _currentPreviewInPoint > TimeSpan.Zero ? _currentPreviewInPoint : (_currentPreviewPosition > TimeSpan.Zero ? _currentPreviewPosition : TimeSpan.Zero);
        if (isLive)
        {
            return (TimeSpan.Zero, specifiedDuration, specifiedDuration);
        }
        var totalDur = specifiedDuration > TimeSpan.Zero ? specifiedDuration : (_currentPreviewDuration > TimeSpan.Zero ? _currentPreviewDuration : inPoint + TimeSpan.FromHours(1));
        var outPoint = totalDur > inPoint ? totalDur : inPoint + TimeSpan.FromHours(1);
        return (inPoint, outPoint, totalDur);
    }

    private PlaylistItem CreateUrlItem()
    {
        var url = UrlBox.Text.Trim();
        if (url.Length == 0 || url.Equals("rtsp://", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Enter a URL/stream address.");
        var duration = ParseDuration(UrlDurationBox.Text);
        var live = UrlLiveCheck.IsChecked == true;
        var probe = live ? null : _urlProbe;
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, live);
        var targetPath = (!string.IsNullOrWhiteSpace(_originalWebPageUrl) && YtDlpResolver.LooksLikeWebVideoPage(_originalWebPageUrl))
            ? _originalWebPageUrl
            : url;
        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, "URL INPUT"), EventType = live ? "LIVE" : "URL",
            SourceKind = "URL", FilePath = targetPath, ResolvedMediaUrl = _resolvedWebVideoUrl ?? string.Empty, AlternateAudioUrl = _resolvedWebAudioUrl ?? string.Empty, InputFormat = UrlFormatBox.Text.Trim(), IsLiveSource = live,
            SourceDuration = sourceDuration, InPoint = inPoint, OutPoint = outPoint,
            SourceFrameRate = probe is { FramesPerSecond: > 0 } ? probe.FramesPerSecond : 25.0,
            Codec = probe?.VideoCodec ?? "Network", VideoFormat = probe is null ? (live ? "Network Live" : "Network VOD") : $"{probe.Width}x{probe.Height} {probe.FramesPerSecond:0.###}p"
        };
    }

    private PlaylistItem CreateNdiItem()
    {
        if (NdiSourceBox.SelectedItem is not NdiDiscoveryService.NdiSourceInfo source || string.IsNullOrWhiteSpace(source.Name))
            throw new InvalidOperationException("Select an NDI source. The source list refreshes every 10 seconds.");
        var duration = ParseDuration(NdiDurationBox.Text);
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, isLive: true);
        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, source.Name), EventType = "NDI", Category = "Input",
            SourceKind = "NDI", FilePath = source.Name, InputOptions = source.Address, IsLiveSource = true,
            SourceDuration = sourceDuration, InPoint = inPoint, OutPoint = outPoint, SourceFrameRate = 25, VideoFormat = "NDI Network"
        };
    }

    private PlaylistItem CreateCaptureItem()
    {
        var video = GetComboText(VideoDeviceBox);
        var audio = GetComboText(AudioDeviceBox);
        if (string.IsNullOrWhiteSpace(video)) throw new InvalidOperationException("Select a video/capture device.");
        ParseResolution(GetComboText(CaptureResolutionBox), out var width, out var height);
        var fps = GetComboText(CaptureFpsBox).StartsWith("AUTO", StringComparison.OrdinalIgnoreCase) ? 0 : ParseDouble(GetComboText(CaptureFpsBox), 25);
        var duration = ParseDuration(CaptureDurationBox.Text);
        var formatText = width > 0 && height > 0 ? $"{width}x{height} " + (fps > 0 ? $"{fps:0.###}p" : "device fps") : "Device Default";
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, isLive: true);
        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, "CAPTURE INPUT"), EventType = "CARD", SourceKind = "DirectShow",
            FilePath = video, VideoDevice = video, AudioDevice = audio, InputFormat = "dshow", IsLiveSource = true,
            SourceDuration = sourceDuration, InPoint = inPoint, OutPoint = outPoint, CaptureWidth = width, CaptureHeight = height, SourceFrameRate = fps,
            Codec = "Windows Capture", VideoFormat = formatText
        };
    }

    private PlaylistItem CreateScreenItem()
    {
        var fps = ParseDouble(GetComboText(ScreenFpsBox), 25);
        var duration = ParseDuration(ScreenDurationBox.Text);
        var options = DrawMouseCheck.IsChecked == true ? "draw_mouse=1" : "draw_mouse=0";
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, isLive: true);
        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, "DESKTOP INPUT"), EventType = "SCREEN", SourceKind = "Screen", FilePath = "desktop", InputFormat = "gdigrab", InputOptions = options, IsLiveSource = true,
            SourceDuration = sourceDuration, InPoint = inPoint, OutPoint = outPoint, SourceFrameRate = fps, Codec = "Desktop Capture", VideoFormat = $"Desktop {fps:0.###}p"
        };
    }

    private PlaylistItem CreateCustomItem()
    {
        var source = CustomSourceBox.Text.Trim();
        if (source.Length == 0) throw new InvalidOperationException("Enter a custom source address.");
        var live = CustomLiveCheck.IsChecked == true;
        var duration = ParseDuration(CustomDurationBox.Text);
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, live);
        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, "CUSTOM INPUT"), EventType = live ? "LIVE" : "INPUT", SourceKind = "Custom",
            FilePath = source, InputFormat = CustomFormatBox.Text.Trim(), InputOptions = CustomOptionsBox.Text.Trim(), IsLiveSource = live,
            SourceDuration = sourceDuration, InPoint = inPoint, OutPoint = outPoint, Codec = "External Source", VideoFormat = "External Source"
        };
    }

    private void StudioMode_Changed(object sender, RoutedEventArgs e)
    {
        if (StudioLocalPanel == null || StudioRemotePanel == null) return;
        var isRemote = StudioModeRemoteRadio?.IsChecked == true;
        StudioLocalPanel.Visibility = isRemote ? Visibility.Collapsed : Visibility.Visible;
        StudioRemotePanel.Visibility = isRemote ? Visibility.Visible : Visibility.Collapsed;
    }

    private PlaylistItem CreateStudioItem()
    {
        var duration = ParseDuration(StudioDurationBox.Text);
        var (inPoint, outPoint, sourceDuration) = CalculatePoints(duration, isLive: true);
        var isRemote = StudioModeRemoteRadio?.IsChecked == true;

        if (isRemote)
        {
            var host = string.IsNullOrWhiteSpace(StudioHostBox?.Text) ? "127.0.0.1" : StudioHostBox.Text.Trim();
            var port = string.IsNullOrWhiteSpace(StudioPortBox?.Text) ? "8088" : StudioPortBox.Text.Trim();
            var protoIdx = StudioProtocolCombo?.SelectedIndex ?? 0;

            string sourceKind;
            string filePath;
            string videoFormat;

            switch (protoIdx)
            {
                case 1:
                    sourceKind = "NetworkStream";
                    filePath = $"srt://{host}:{port}?mode=caller&latency=200";
                    videoFormat = $"SRT 1080p Studio Feed ({host}:{port})";
                    break;
                case 2:
                    sourceKind = "NetworkStream";
                    filePath = $"udp://{host}:{port}";
                    videoFormat = $"UDP DVB-TS Studio Feed ({host}:{port})";
                    break;
                case 3:
                    sourceKind = "NetworkStream";
                    filePath = $"rtmp://{host}:{port}/live/studio";
                    videoFormat = $"RTMP Studio Feed ({host}:{port})";
                    break;
                default:
                    sourceKind = "NDI";
                    filePath = $"KashtrixStudio ({host})";
                    videoFormat = $"NDI Studio PGM ({host})";
                    break;
            }

            return new PlaylistItem
            {
                Title = CleanTitle(TitleBox.Text, $"KASHTRIX STUDIO ({host})"),
                EventType = "LIVE",
                Category = "Input",
                SourceKind = sourceKind,
                FilePath = filePath,
                IsLiveSource = true,
                SourceDuration = sourceDuration,
                InPoint = inPoint,
                OutPoint = outPoint,
                SourceFrameRate = 50.0,
                Codec = "Remote Studio Network Stream",
                VideoFormat = videoFormat
            };
        }

        return new PlaylistItem
        {
            Title = CleanTitle(TitleBox.Text, "KASHTRIX STUDIO LIVE"),
            EventType = "LIVE",
            Category = "Input",
            SourceKind = "SharedMemory",
            FilePath = BroadcastPlayout.Outputs.VirtualOutputBridge.StudioProgramVideoMapName,
            AlternateAudioUrl = BroadcastPlayout.Outputs.VirtualOutputBridge.StudioProgramAudioMapName,
            IsLiveSource = true,
            SourceDuration = sourceDuration,
            InPoint = inPoint,
            OutPoint = outPoint,
            SourceFrameRate = 50.0,
            Codec = "Studio Shared Memory",
            VideoFormat = "1920x1080 50p Studio PGM (127.0.0.1)"
        };
    }

    private void ConnectKashtrixStudio_Click(object sender, RoutedEventArgs e)
    {
        SourceTabs.SelectedIndex = 5;
        TitleBox.Text = StudioModeRemoteRadio?.IsChecked == true
            ? $"Kashtrix Studio ({StudioHostBox?.Text?.Trim() ?? "Remote"})"
            : "Kashtrix Studio Live";
        ResultItem = CreateStudioItem();
        DialogResult = true;
    }

    private void LoadDemo_Click(object sender, RoutedEventArgs e)
    {
        SourceTabs.SelectedIndex = 3;
        TitleBox.Text = "Kashtrix Demo Input";
        CustomFormatBox.Text = string.Empty;
        CustomSourceBox.Text = Path.Combine(AppContext.BaseDirectory, "demos", "gfx", "video", "kashtrix-video-layer.mp4");
        CustomOptionsBox.Text = string.Empty;
        CustomLiveCheck.IsChecked = false;
        CustomDurationBox.Text = "00:00:12";
        QueuePreviewRestart();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.ToggleMaximize(this);
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static string Friendly(Exception ex)
    {
        var message = ex.Message.Replace("FFmpeg", "media engine", StringComparison.OrdinalIgnoreCase).Replace("avformat", "media input", StringComparison.OrdinalIgnoreCase);
        if (message.Contains("input format 'dshow'", StringComparison.OrdinalIgnoreCase)) return "Windows capture support was not initialized. Restart Kashtrix after installing the media runtime.";
        return message;
    }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowChromeActions.Drag(this, e);
    private static string CleanTitle(string? text, string fallback) => string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
    private static TimeSpan ParseDuration(string? text) => TimeSpan.TryParse(text, out var value) && value > TimeSpan.Zero ? value : throw new InvalidOperationException("Duration must be HH:MM:SS and greater than zero.");
    private static double ParseDouble(string? text, double fallback) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : fallback;
    private static string GetComboText(ComboBox box) => box.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? "" : box.Text?.Trim() ?? "";
    private static void ParseResolution(string text, out int width, out int height) { var parts = text.Split('x', 'X'); if (parts.Length != 2 || !int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height)) { width = 0; height = 0; } }
    private static string Format(TimeSpan value) => $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
}
