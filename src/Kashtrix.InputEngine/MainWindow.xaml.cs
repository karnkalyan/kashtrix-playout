using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BroadcastPlayout.Ffmpeg;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;

namespace Kashtrix.InputEngine;

public sealed class InputEngineCardModel : INotifyPropertyChanged
{
    private string _title = "Live Source";
    private string _sourceDetails = "";
    private string _statusText = "IDLE";
    private Brush _statusBg = new SolidColorBrush(Color.FromRgb(20, 27, 36));
    private Brush _statusFg = new SolidColorBrush(Color.FromRgb(140, 155, 168));
    private Brush _borderBrush = new SolidColorBrush(Color.FromRgb(34, 43, 54));
    private string _iconGlyph = "📡";
    private string _formatLabel = "LIVE FEED";
    private string _resolutionInfo = "Awaiting signal...";
    private ImageSource? _previewImage;
    private double _audioLevelL;
    private double _audioLevelR;
    private string _audioDbL = "-∞ dB";
    private string _audioDbR = "-∞ dB";
    private Brush _meterBrushL = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    private Brush _meterBrushR = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    private string _routeStatusText = "READY";
    private Brush _routeStatusColor = new SolidColorBrush(Color.FromRgb(140, 155, 168));
    private bool _isPlaying;
    private bool _isPaused;
    private bool _isStopped = true;
    private bool _isMuted;
    private bool _isPushed;

    public bool IsMuted
    {
        get => _isMuted;
        set { if (Set(ref _isMuted, value)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MuteLabel))); }
    }
    public bool IsPushed
    {
        get => _isPushed;
        set
        {
            if (Set(ref _isPushed, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PushButtonText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PushButtonBg)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BorderBrush)));
            }
        }
    }
    public string PushButtonText => _isPushed ? "REMOVE FROM PLAYOUT" : "PUSH TO PLAYOUT";
    public Brush PushButtonBg => _isPushed ? new SolidColorBrush(Color.FromRgb(185, 28, 28)) : new SolidColorBrush(Color.FromRgb(107, 52, 166));
    public string MuteLabel => _isMuted ? "🔇 UNMUTE" : "🔊 MUTE";

    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceKind { get; set; } = "Url";
    public string FilePath { get; set; } = "";
    public string InputOptions { get; set; } = "";
    public string VideoDevice { get; set; } = "";
    public string AudioDevice { get; set; } = "";
    public string AlternateAudioUrl { get; set; } = "";
    public TimeSpan SourceDuration { get; set; } = TimeSpan.Zero;
    public TimeSpan CurrentPosition { get; set; } = TimeSpan.Zero;
    public double SourceFrameRate { get; set; } = 25.0;

    public bool IsPlaying { get => _isPlaying; set => Set(ref _isPlaying, value); }
    public bool IsPaused { get => _isPaused; set => Set(ref _isPaused, value); }
    public bool IsStopped { get => _isStopped; set => Set(ref _isStopped, value); }

    public string Title { get => _title; set => Set(ref _title, value); }
    public string SourceDetails { get => _sourceDetails; set => Set(ref _sourceDetails, value); }
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public Brush StatusBg { get => _statusBg; set => Set(ref _statusBg, value); }
    public Brush StatusFg { get => _statusFg; set => Set(ref _statusFg, value); }
    public Brush BorderBrush { get => _borderBrush; set => Set(ref _borderBrush, value); }
    public string IconGlyph { get => _iconGlyph; set => Set(ref _iconGlyph, value); }
    public string FormatLabel { get => _formatLabel; set => Set(ref _formatLabel, value); }
    public string ResolutionInfo { get => _resolutionInfo; set => Set(ref _resolutionInfo, value); }

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        set
        {
            if (Set(ref _previewImage, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlaceholderVisibility)));
            }
        }
    }

    public Visibility PlaceholderVisibility => _previewImage == null ? Visibility.Visible : Visibility.Collapsed;

    public double AudioLevelL { get => _audioLevelL; set => Set(ref _audioLevelL, value); }
    public double AudioLevelR { get => _audioLevelR; set => Set(ref _audioLevelR, value); }
    public string AudioDbL { get => _audioDbL; set => Set(ref _audioDbL, value); }
    public string AudioDbR { get => _audioDbR; set => Set(ref _audioDbR, value); }
    public Brush MeterBrushL { get => _meterBrushL; set => Set(ref _meterBrushL, value); }
    public Brush MeterBrushR { get => _meterBrushR; set => Set(ref _meterBrushR, value); }
    public string RouteStatusText { get => _routeStatusText; set => Set(ref _routeStatusText, value); }
    public Brush RouteStatusColor { get => _routeStatusColor; set => Set(ref _routeStatusColor, value); }

    [System.Text.Json.Serialization.JsonIgnore]
    public CancellationTokenSource? PipelineCts { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        return true;
    }
}

public sealed record RoutedInputPayload(
    Guid Id,
    string Title,
    string SourceKind,
    string FilePath,
    string InputOptions,
    string VideoDevice,
    string AudioDevice,
    string AlternateAudioUrl,
    string FormatLabel,
    double SourceFrameRate,
    DateTime TimestampUtc,
    string Action = "Push"
);

public sealed record PersistedSourceRecord(
    Guid Id,
    string Title,
    string SourceKind,
    string FilePath,
    string InputOptions,
    string VideoDevice,
    string AudioDevice,
    string AlternateAudioUrl,
    double SourceFrameRate,
    string FormatLabel
);

public partial class MainWindow : Window
{
    private readonly ObservableCollection<InputEngineCardModel> _cards = [];
    private InputEngineCardModel? _selectedCard;

    private static readonly Brush _normalBorderBrush = new SolidColorBrush(Color.FromRgb(34, 43, 54));
    private static readonly Brush _selectedBorderBrush = new SolidColorBrush(Color.FromRgb(128, 85, 179));
    private static readonly Brush _greenMeterBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94));
    private static readonly Brush _amberMeterBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));
    private static readonly Brush _redMeterBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
    private static readonly Brush _pushedBorderBrush = new SolidColorBrush(Color.FromRgb(229, 57, 53));

    private void ApplyBorders()
    {
        foreach (var c in _cards)
        {
            c.BorderBrush = c.IsPushed ? _pushedBorderBrush
                          : ReferenceEquals(c, _selectedCard) ? _selectedBorderBrush
                          : _normalBorderBrush;
        }
    }

    private void CardMute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            card.IsMuted = !card.IsMuted;
            LastActionText.Text = (card.IsMuted ? "Muted: " : "Unmuted: ") + card.Title;
        }
    }

    private void CardFullScreen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            var win = new FullScreenInputWindow(card) { Owner = this };
            win.ShowDialog();
        }
    }

    private void CardEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            EditCard(card);
        }
    }

    private void EditCard(InputEngineCardModel card)
    {
        var dlg = new BroadcastPlayout.Views.InputSourceWindow { Owner = this };
        dlg.LoadItem(ToPlaylistItem(card));
        if (dlg.ShowDialog() != true || dlg.ResultItem is not { } item) return;
        StopCardPipeline(card, retainStatus: false);
        card.Title = item.Title;
        card.SourceKind = item.SourceKind;
        card.FilePath = item.FilePath ?? "";
        card.InputOptions = item.InputOptions ?? "";
        card.VideoDevice = item.VideoDevice ?? "";
        card.AudioDevice = item.AudioDevice ?? "";
        card.AlternateAudioUrl = item.AlternateAudioUrl ?? "";
        card.SourceFrameRate = item.SourceFrameRate > 0 ? item.SourceFrameRate : 25.0;
        card.SourceDetails = !string.IsNullOrWhiteSpace(item.FilePath) ? item.FilePath : item.Title;
        SaveSourcesToDisk();
        StartCardPipeline(card);
        LastActionText.Text = $"Updated input: {card.Title}";
    }

    private async void ResolveCardTitleAsync(InputEngineCardModel card)
    {
        try
        {
            var url = card.FilePath;
            if (!YtDlpResolver.LooksLikeWebVideoPage(url)) return;
            var result = await YtDlpResolver.ResolveAsync(url);
            if (!string.IsNullOrWhiteSpace(result.Title)) { card.Title = result.Title; SaveSourcesToDisk(); }
        }
        catch { }
    }

    private static bool IsGenericTitle(string? t) =>
        string.IsNullOrWhiteSpace(t) || t.Equals("Live Input", StringComparison.OrdinalIgnoreCase) ||
        t.Equals("URL INPUT", StringComparison.OrdinalIgnoreCase) || t.Equals("Live Source", StringComparison.OrdinalIgnoreCase);

    private static string ConfigFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kashtrix", "input_sources.json");

    public MainWindow()
    {
        InitializeComponent();
        InputSourcesGrid.ItemsSource = _cards;

        LoadSavedSources();
        UpdateCountDisplay();
    }

    private void UpdateCountDisplay()
    {
        InputCountText.Text = $"{_cards.Count} SOURCES CONFIGURED";
        EmptyStatePanel.Visibility = _cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadSavedSources()
    {
        try
        {
            var file = ConfigFilePath;
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                var records = JsonSerializer.Deserialize<PersistedSourceRecord[]>(json);
                if (records != null)
                {
                    foreach (var rec in records)
                    {
                        var icon = rec.SourceKind switch
                        {
                            "DirectShow" or "Device" => "🎥",
                            "URL" or "Url" => "📡",
                            "NDI" or "Ndi" => "🌐",
                            "Screen" => "🖥",
                            _ => "⚡"
                        };
                        var details = !string.IsNullOrWhiteSpace(rec.FilePath) ? rec.FilePath :
                                      !string.IsNullOrWhiteSpace(rec.VideoDevice) ? rec.VideoDevice :
                                      rec.Title;

                        var card = new InputEngineCardModel
                        {
                            Id = rec.Id,
                            Title = rec.Title,
                            SourceKind = rec.SourceKind,
                            FilePath = rec.FilePath ?? "",
                            InputOptions = rec.InputOptions ?? "",
                            VideoDevice = rec.VideoDevice ?? "",
                            AudioDevice = rec.AudioDevice ?? "",
                            AlternateAudioUrl = rec.AlternateAudioUrl ?? "",
                            SourceFrameRate = rec.SourceFrameRate > 0 ? rec.SourceFrameRate : 25.0,
                            SourceDetails = details,
                            IconGlyph = icon,
                            FormatLabel = !string.IsNullOrWhiteSpace(rec.FormatLabel) ? rec.FormatLabel : $"{rec.SourceKind.ToUpperInvariant()} FEED",
                            ResolutionInfo = "Ready to start feed",
                            StatusText = "STANDBY",
                            StatusBg = new SolidColorBrush(Color.FromRgb(20, 27, 36)),
                            StatusFg = new SolidColorBrush(Color.FromRgb(140, 155, 168)),
                            BorderBrush = _normalBorderBrush
                        };
                        _cards.Add(card);
                        if (IsGenericTitle(card.Title)) ResolveCardTitleAsync(card);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LastActionText.Text = $"Error reading saved sources: {ex.Message}";
        }

        if (_cards.Count > 0)
        {
            SelectCard(_cards[0]);
        }
    }

    private void SaveSourcesToDisk()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

            var records = _cards.Select(c => new PersistedSourceRecord(
                c.Id,
                c.Title,
                c.SourceKind,
                c.FilePath,
                c.InputOptions,
                c.VideoDevice,
                c.AudioDevice,
                c.AlternateAudioUrl,
                c.SourceFrameRate,
                c.FormatLabel
            )).ToArray();

            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
        catch { }
    }

    private void SelectCard(InputEngineCardModel card)
    {
        _selectedCard = card;
        ApplyBorders();
    }

    private void AddStudioSource_Click(object sender, RoutedEventArgs e)
    {
        var item = new PlaylistItem
        {
            Title = "Kashtrix Studio Live Program",
            EventType = "LIVE",
            Category = "Input",
            SourceKind = "SharedMemory",
            FilePath = BroadcastPlayout.Outputs.VirtualOutputBridge.StudioProgramVideoMapName,
            AlternateAudioUrl = BroadcastPlayout.Outputs.VirtualOutputBridge.StudioProgramAudioMapName,
            IsLiveSource = true,
            SourceDuration = TimeSpan.FromHours(4),
            InPoint = TimeSpan.Zero,
            OutPoint = TimeSpan.FromHours(4),
            SourceFrameRate = 50.0,
            Codec = "Studio Shared Memory",
            VideoFormat = "1920x1080 50p Studio PGM"
        };
        AddCardFromPlaylistItem(item);
    }

    private void AddSource_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new BroadcastPlayout.Views.InputSourceWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.ResultItem is not null)
        {
            AddCardFromPlaylistItem(dlg.ResultItem);
        }
    }

    private void AddCardFromPlaylistItem(PlaylistItem item)
    {
        var icon = item.SourceKind switch
        {
            "DirectShow" or "Device" => "🎥",
            "URL" or "Url" => "📡",
            "NDI" or "Ndi" => "🌐",
            "Screen" => "🖥",
            "SharedMemory" or "KashtrixStudio" => "🎬",
            _ => "⚡"
        };
        var format = !string.IsNullOrWhiteSpace(item.VideoFormat) ? item.VideoFormat :
                     !string.IsNullOrWhiteSpace(item.InputFormat) ? item.InputFormat :
                     $"{item.SourceKind} LIVE";
        var details = !string.IsNullOrWhiteSpace(item.FilePath) ? item.FilePath :
                      !string.IsNullOrWhiteSpace(item.VideoDevice) ? item.VideoDevice :
                      item.Title;

        var newCard = new InputEngineCardModel
        {
            Id = item.Id,
            Title = item.Title,
            SourceDetails = details,
            SourceKind = item.SourceKind,
            FilePath = item.FilePath ?? "",
            InputOptions = item.InputOptions ?? "",
            VideoDevice = item.VideoDevice ?? "",
            AudioDevice = item.AudioDevice ?? "",
            AlternateAudioUrl = item.AlternateAudioUrl ?? "",
            SourceDuration = item.SourceDuration,
            SourceFrameRate = item.SourceFrameRate > 0 ? item.SourceFrameRate : 25.0,
            IconGlyph = icon,
            FormatLabel = format.ToUpperInvariant(),
            ResolutionInfo = "Connecting...",
            StatusText = "STARTING",
            StatusBg = new SolidColorBrush(Color.FromRgb(20, 35, 55)),
            StatusFg = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
            BorderBrush = _normalBorderBrush
        };

        _cards.Add(newCard);
        if (IsGenericTitle(newCard.Title)) ResolveCardTitleAsync(newCard);
        SelectCard(newCard);
        UpdateCountDisplay();
        SaveSourcesToDisk();

        StartCardPipeline(newCard);
        LastActionText.Text = $"Added source: {newCard.Title} ({newCard.SourceKind})";
    }

    private static PlaylistItem ToPlaylistItem(InputEngineCardModel card)
    {
        var live = card.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) ||
                   card.SourceKind.Equals("Device", StringComparison.OrdinalIgnoreCase) ||
                   card.SourceKind.Equals("NDI", StringComparison.OrdinalIgnoreCase) ||
                   card.SourceKind.Equals("Screen", StringComparison.OrdinalIgnoreCase) ||
                   card.FilePath.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) ||
                   card.FilePath.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) ||
                   card.FilePath.StartsWith("srt://", StringComparison.OrdinalIgnoreCase) ||
                   card.FilePath.StartsWith("udp://", StringComparison.OrdinalIgnoreCase);

        var inPoint = !live && card.CurrentPosition > TimeSpan.Zero ? card.CurrentPosition : TimeSpan.Zero;
        var totalDur = card.SourceDuration > inPoint ? card.SourceDuration : (live ? TimeSpan.FromHours(24) : inPoint + TimeSpan.FromHours(1));
        var outPoint = totalDur > inPoint ? totalDur : inPoint + TimeSpan.FromHours(1);

        return new PlaylistItem
        {
            Id = card.Id,
            Title = card.Title,
            SourceKind = card.SourceKind,
            FilePath = card.FilePath,
            InputOptions = card.InputOptions,
            VideoDevice = card.VideoDevice,
            AudioDevice = card.AudioDevice,
            AlternateAudioUrl = card.AlternateAudioUrl,
            IsLiveSource = live,
            SourceDuration = totalDur,
            InPoint = inPoint,
            OutPoint = outPoint,
            SourceFrameRate = card.SourceFrameRate > 0 ? card.SourceFrameRate : 25.0
        };
    }

    private async void StartCardPipeline(InputEngineCardModel card)
    {
        StopCardPipeline(card, retainStatus: false);

        card.IsPlaying = true;
        card.IsPaused = false;
        card.IsStopped = false;
        card.StatusText = "CONNECTING";
        card.StatusBg = new SolidColorBrush(Color.FromRgb(20, 35, 55));
        card.StatusFg = new SolidColorBrush(Color.FromRgb(56, 189, 248));

        var cts = new CancellationTokenSource();
        card.PipelineCts = cts;

        var item = ToPlaylistItem(card);

        if (YtDlpResolver.LooksLikeWebVideoPage(item.FilePath))
        {
            try
            {
                card.ResolutionInfo = "Resolving web video streams...";
                var resolved = await YtDlpResolver.ResolveAsync(item.FilePath, cts.Token);
                item.FilePath = resolved.MediaUrl;
                if (!string.IsNullOrWhiteSpace(resolved.AudioUrl))
                {
                    item.AlternateAudioUrl = resolved.AudioUrl;
                    card.AlternateAudioUrl = resolved.AudioUrl;
                }
                card.ResolutionInfo = "Web stream resolved";
            }
            catch (Exception ex)
            {
                if (cts.IsCancellationRequested) return;
                card.StatusText = "NO SIGNAL";
                card.StatusBg = new SolidColorBrush(Color.FromRgb(60, 20, 20));
                card.StatusFg = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                card.ResolutionInfo = ShortMessage(ex.Message);
                return;
            }
        }

        var avGate = new ManualResetEventSlim(false);
        var clock = new Stopwatch();

        // 1. Video decoding & live preview rendering task
        _ = Task.Run(() =>
        {
            try
            {
                do
                {
                    using var videoDecoder = new VideoDecoder(item);
                    var firstPts = double.NaN;
                    var lastPublish = DateTime.MinValue;

                    while (!cts.IsCancellationRequested && videoDecoder.TryRead(out var frame))
                    {
                        while (!cts.IsCancellationRequested && card.IsPaused)
                        {
                            Thread.Sleep(30);
                        }

                        if (!item.IsLiveSource)
                        {
                            if (double.IsNaN(firstPts))
                            {
                                firstPts = frame.PtsSeconds;
                                clock.Restart();
                                avGate.Set();
                            }
                            var target = Math.Max(0, frame.PtsSeconds - firstPts);
                            var elapsed = clock.Elapsed.TotalSeconds;
                            if (target > elapsed)
                            {
                                var waitMs = (int)((target - elapsed) * 1000);
                                if (waitMs > 1) Thread.Sleep(Math.Min(waitMs, 40));
                            }
                        }
                        else
                        {
                            if (double.IsNaN(firstPts))
                            {
                                firstPts = frame.PtsSeconds;
                                clock.Restart();
                                avGate.Set();
                            }
                        }

                        card.CurrentPosition = TimeSpan.FromSeconds(Math.Max(0, frame.PtsSeconds));

                        var now = DateTime.UtcNow;
                        if ((now - lastPublish).TotalMilliseconds < 30) continue;
                        lastPublish = now;

                        var bmp = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, frame.Bgra, frame.Stride);
                        bmp.Freeze();

                        SafeBeginInvoke(() =>
                        {
                            if (cts.IsCancellationRequested) return;
                            card.PreviewImage = bmp;
                            card.ResolutionInfo = $"{frame.Width}×{frame.Height} · {frame.FramesPerSecond:0.##}p";
                            card.StatusText = "LIVE SIGNAL";
                            card.StatusBg = new SolidColorBrush(Color.FromRgb(20, 83, 45));
                            card.StatusFg = new SolidColorBrush(Color.FromRgb(74, 222, 128));
                        });
                    }
                } while (!cts.IsCancellationRequested && !item.IsLiveSource && card.IsPlaying);
            }
            catch (Exception ex)
            {
                SafeBeginInvoke(() =>
                {
                    if (cts.IsCancellationRequested) return;
                    card.StatusText = "NO SIGNAL";
                    card.StatusBg = new SolidColorBrush(Color.FromRgb(60, 20, 20));
                    card.StatusFg = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                    card.ResolutionInfo = ShortMessage(ex.Message);
                });
            }
            finally
            {
                avGate.Set();
            }
        });

        // 2. Audio decoding & true measurement task with synchronized pacing and broadcast ballistics
        _ = Task.Run(() =>
        {
            if (item.SourceKind.Equals("Screen", StringComparison.OrdinalIgnoreCase)) return;
            if (item.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(item.AudioDevice)) return;
            try
            {
                using var localAudio = new BroadcastPlayout.Outputs.LocalAudioOutput();
                do
                {
                    using var audioDecoder = new AudioDecoder(item);

                    // Pre-read initial audio chunk so network connect & demuxing is ready
                    AudioChunk firstChunk = null!;
                    var preRetries = 0;
                    while (!audioDecoder.TryRead(out firstChunk))
                    {
                        if (cts.IsCancellationRequested) return;
                        if ((item.IsLiveSource || item.SourceKind.Equals("URL", StringComparison.OrdinalIgnoreCase)) && preRetries++ < 35)
                        {
                            Thread.Sleep(20);
                            continue;
                        }
                        return;
                    }

                    // Wait for video frame 0 to trigger start gate
                    try { avGate.Wait(TimeSpan.FromSeconds(5), cts.Token); }
                    catch (OperationCanceledException) { return; }

                    var lastAudioUiPublish = DateTime.MinValue;
                    double smoothL = 0;
                    double smoothR = 0;
                    var firstAudioPts = double.NaN;
                    var isFirst = true;
                    AudioChunk chunk = firstChunk;

                    while (!cts.IsCancellationRequested)
                    {
                        if (!isFirst)
                        {
                            if (!audioDecoder.TryRead(out chunk)) break;
                        }
                        isFirst = false;

                        while (!cts.IsCancellationRequested && card.IsPaused)
                        {
                            Thread.Sleep(30);
                        }

                        // Feed decoded audio samples to local audio output for live listening
                        if (!card.IsMuted) localAudio.Send(chunk);

                        var (l, r) = MeasurePcmPeak(chunk.Pcm16Stereo48k);

                        if (!item.IsLiveSource)
                        {
                            if (double.IsNaN(firstAudioPts)) firstAudioPts = chunk.PtsSeconds;
                            var target = Math.Max(0, chunk.PtsSeconds - firstAudioPts);
                            var elapsed = clock.Elapsed.TotalSeconds;
                            var wait = target - elapsed;
                            if (wait > 0.002)
                            {
                                var waitMs = (int)(wait * 1000.0);
                                if (waitMs > 1) Thread.Sleep(Math.Min(waitMs, 40));
                            }
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }

                        // Broadcast PPM ballistics: fast attack, smooth decay
                        smoothL = Math.Max(l * 100.0, smoothL * 0.88);
                        smoothR = Math.Max(r * 100.0, smoothR * 0.88);

                        var now = DateTime.UtcNow;
                        if ((now - lastAudioUiPublish).TotalMilliseconds < 33) continue;
                        lastAudioUiPublish = now;

                        var levelL = smoothL;
                        var levelR = smoothR;
                        var rawL = levelL / 100.0;
                        var rawR = levelR / 100.0;
                        var dbL = rawL > 0.0001 ? Math.Max(-60.0, 20.0 * Math.Log10(rawL)) : double.NegativeInfinity;
                        var dbR = rawR > 0.0001 ? Math.Max(-60.0, 20.0 * Math.Log10(rawR)) : double.NegativeInfinity;
                        var textL = double.IsNegativeInfinity(dbL) ? "-∞ dB" : $"{dbL:0.0} dB";
                        var textR = double.IsNegativeInfinity(dbR) ? "-∞ dB" : $"{dbR:0.0} dB";

                        SafeBeginInvoke(() =>
                        {
                            if (cts.IsCancellationRequested) return;
                            card.AudioLevelL = levelL;
                            card.AudioLevelR = levelR;
                            card.AudioDbL = textL;
                            card.AudioDbR = textR;

                            card.MeterBrushL = levelL > 85 ? _redMeterBrush : (levelL > 65 ? _amberMeterBrush : _greenMeterBrush);
                            card.MeterBrushR = levelR > 85 ? _redMeterBrush : (levelR > 65 ? _amberMeterBrush : _greenMeterBrush);
                        });
                    }
                } while (!cts.IsCancellationRequested && !item.IsLiveSource && card.IsPlaying);
            }
            catch
            {
                SafeBeginInvoke(() =>
                {
                    if (cts.IsCancellationRequested) return;
                    card.AudioLevelL = 0;
                    card.AudioLevelR = 0;
                    card.AudioDbL = "-∞ dB";
                    card.AudioDbR = "-∞ dB";
                });
            }
        });
    }

    private void StopCardPipeline(InputEngineCardModel card, bool retainStatus = false)
    {
        var old = card.PipelineCts;
        card.PipelineCts = null;
        if (old != null)
        {
            try { old.Cancel(); } catch { }
            try { old.Dispose(); } catch { }
        }

        if (!retainStatus)
        {
            card.PreviewImage = null;
            card.AudioLevelL = 0;
            card.AudioLevelR = 0;
            card.AudioDbL = "-∞ dB";
            card.AudioDbR = "-∞ dB";
            card.IsPlaying = false;
            card.IsPaused = false;
            card.IsStopped = true;
            card.StatusText = "STOPPED";
            card.StatusBg = new SolidColorBrush(Color.FromRgb(20, 27, 36));
            card.StatusFg = new SolidColorBrush(Color.FromRgb(140, 155, 168));
        }
    }

    private static (double Left, double Right) MeasurePcmPeak(byte[]? pcm)
    {
        if (pcm == null || pcm.Length < 4) return (0, 0);
        var l = 0; var r = 0;
        for (var i = 0; i + 3 < pcm.Length; i += 4)
        {
            l = Math.Max(l, Math.Abs((int)BitConverter.ToInt16(pcm, i)));
            r = Math.Max(r, Math.Abs((int)BitConverter.ToInt16(pcm, i + 2)));
        }
        return (Math.Clamp(l / 32768.0, 0, 1), Math.Clamp(r / 32768.0, 0, 1));
    }

    private void CardPlay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            SelectCard(card);
            if (card.IsPaused && card.PipelineCts != null && !card.PipelineCts.IsCancellationRequested)
            {
                card.IsPaused = false;
                card.IsPlaying = true;
                card.StatusText = "LIVE SIGNAL";
                card.StatusBg = new SolidColorBrush(Color.FromRgb(20, 83, 45));
                card.StatusFg = new SolidColorBrush(Color.FromRgb(74, 222, 128));
                LastActionText.Text = $"Resumed input: {card.Title}";
            }
            else
            {
                StartCardPipeline(card);
                LastActionText.Text = $"Started feed: {card.Title}";
            }
        }
    }

    private void CardPause_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            SelectCard(card);
            card.IsPaused = true;
            card.IsPlaying = false;
            card.StatusText = "PAUSED";
            card.StatusBg = new SolidColorBrush(Color.FromRgb(70, 45, 10));
            card.StatusFg = new SolidColorBrush(Color.FromRgb(245, 158, 11));
            LastActionText.Text = $"Paused feed: {card.Title}";
        }
    }

    private void CardStop_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            SelectCard(card);
            StopCardPipeline(card, retainStatus: false);
            LastActionText.Text = $"Stopped feed: {card.Title}";
        }
    }

    private void DeleteCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            StopCardPipeline(card, retainStatus: false);
            _cards.Remove(card);
            if (_selectedCard == card) _selectedCard = _cards.FirstOrDefault();
            UpdateCountDisplay();
            SaveSourcesToDisk();
            LastActionText.Text = $"Removed source: {card.Title}";
        }
    }

    private void InputCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is InputEngineCardModel card)
        {
            SelectCard(card);
            if (e.ClickCount >= 2 && e.OriginalSource is not System.Windows.Controls.Primitives.ButtonBase)
            {
                e.Handled = true;
                EditCard(card);
            }
        }
    }

    private void RouteToPlayout_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard is null && _cards.Count > 0) SelectCard(_cards[0]);
        if (_selectedCard is not null)
        {
            PushSourceToPlayout(_selectedCard);
        }
    }

    private void PushCardSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is InputEngineCardModel card)
        {
            SelectCard(card);
            if (card.IsPushed)
            {
                RemoveSourceFromPlayout(card);
            }
            else
            {
                PushSourceToPlayout(card);
            }
        }
    }

    private void PushSourceToPlayout(InputEngineCardModel card)
    {
        try
        {
            var isHardware = card.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) ||
                             card.SourceKind.Equals("Device", StringComparison.OrdinalIgnoreCase);

            if (isHardware)
            {
                // Exclusive DirectShow capture hardware pin lock:
                // Stop the local pipeline in InputEngine so Playout can open the webcam without I/O error (-5 / 0x8007001F)
                StopCardPipeline(card, retainStatus: true);
                card.StatusText = "ROUTED / HW RELEASED";
                card.StatusBg = new SolidColorBrush(Color.FromRgb(20, 35, 55));
                card.StatusFg = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                card.ResolutionInfo = "DirectShow pin freed for Playout";
            }

            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kashtrix");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, "routed_input.json");

            var payload = new RoutedInputPayload(
                card.Id,
                card.Title,
                card.SourceKind,
                card.FilePath,
                card.InputOptions,
                card.VideoDevice,
                card.AudioDevice,
                card.AlternateAudioUrl,
                card.FormatLabel,
                card.SourceFrameRate > 0 ? card.SourceFrameRate : 25.0,
                DateTime.UtcNow,
                "Push"
            );

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            card.RouteStatusText = "ROUTED TO PLAYOUT";
            card.RouteStatusColor = new SolidColorBrush(Color.FromRgb(229, 57, 53));
            foreach (var other in _cards) other.IsPushed = ReferenceEquals(other, card);
            ApplyBorders();
            foreach (var other in _cards.Where(o => !ReferenceEquals(o, card) && o.RouteStatusText == "ROUTED TO PLAYOUT")) { other.RouteStatusText = "READY"; other.RouteStatusColor = new SolidColorBrush(Color.FromRgb(140, 155, 168)); }
            LastActionText.Text = isHardware
                ? $"Successfully routed '{card.Title}' to Playout (Hardware pin released for Playout engine)."
                : $"Successfully routed '{card.Title}' to Kashtrix Playout.";
        }
        catch (Exception ex)
        {
            LastActionText.Text = $"Error routing to Playout: {ex.Message}";
        }
    }

    private void RemoveSourceFromPlayout(InputEngineCardModel card)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kashtrix");
            Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, "routed_input.json");

            var payload = new RoutedInputPayload(
                card.Id,
                card.Title,
                card.SourceKind,
                card.FilePath,
                card.InputOptions,
                card.VideoDevice,
                card.AudioDevice,
                card.AlternateAudioUrl,
                card.FormatLabel,
                card.SourceFrameRate > 0 ? card.SourceFrameRate : 25.0,
                DateTime.UtcNow,
                "Remove"
            );

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            card.IsPushed = false;
            card.RouteStatusText = "READY";
            card.RouteStatusColor = new SolidColorBrush(Color.FromRgb(140, 155, 168));
            ApplyBorders();

            var isHardware = card.SourceKind.Equals("DirectShow", StringComparison.OrdinalIgnoreCase) ||
                             card.SourceKind.Equals("Device", StringComparison.OrdinalIgnoreCase);

            if (isHardware)
            {
                StartCardPipeline(card);
            }

            LastActionText.Text = $"Removed '{card.Title}' from Playout.";
        }
        catch (Exception ex)
        {
            LastActionText.Text = $"Error removing from Playout: {ex.Message}";
        }
    }

    private void SafeBeginInvoke(Action action)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        try { Dispatcher.BeginInvoke(action); } catch { }
    }

    private static string ShortMessage(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Signal unavailable" : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return text.Length <= 48 ? text : text[..45] + "...";
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) Maximize_Click(sender, e);
        else DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        foreach (var card in _cards)
        {
            StopCardPipeline(card, retainStatus: false);
        }
    }
}
