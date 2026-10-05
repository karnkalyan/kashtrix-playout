using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public sealed class EngineItemModel : INotifyPropertyChanged
{
    private string _statusText = "STOPPED";
    private Brush _statusBg = new SolidColorBrush(Color.FromRgb(20, 24, 30));
    private Brush _statusBorder = new SolidColorBrush(Color.FromRgb(40, 48, 60));
    private Brush _statusFg = new SolidColorBrush(Color.FromRgb(140, 150, 160));
    private Brush _statusDotBrush = new SolidColorBrush(Color.FromRgb(100, 110, 120));
    private Brush _cardBorderBrush = new SolidColorBrush(Color.FromRgb(28, 36, 46));
    private string _telemetry = "Process inactive · not started";

    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string Description { get; init; } = "";
    public string ProcessName { get; init; } = "";
    public string IconGlyph { get; init; } = "⚙";
    public Brush AccentBrush { get; init; } = new SolidColorBrush(Color.FromRgb(177, 107, 228));
    public bool IsInternalPlayout { get; init; }

    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public Brush StatusBg { get => _statusBg; set => Set(ref _statusBg, value); }
    public Brush StatusBorder { get => _statusBorder; set => Set(ref _statusBorder, value); }
    public Brush StatusFg { get => _statusFg; set => Set(ref _statusFg, value); }
    public Brush StatusDotBrush { get => _statusDotBrush; set => Set(ref _statusDotBrush, value); }
    public Brush CardBorderBrush { get => _cardBorderBrush; set => Set(ref _cardBorderBrush, value); }
    public string Telemetry { get => _telemetry; set => Set(ref _telemetry, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        return true;
    }
}

public partial class EngineDashboardWindow : Window
{
    private readonly ObservableCollection<EngineItemModel> _engines = [];
    private readonly DispatcherTimer _pollTimer;

    private static readonly Brush GreenDot = new SolidColorBrush(Color.FromRgb(85, 200, 120));
    private static readonly Brush GrayDot = new SolidColorBrush(Color.FromRgb(100, 110, 120));
    private static readonly Brush PurpleDot = new SolidColorBrush(Color.FromRgb(177, 107, 228));

    private static readonly Brush RunningBg = new SolidColorBrush(Color.FromRgb(16, 38, 26));
    private static readonly Brush RunningBorder = new SolidColorBrush(Color.FromRgb(40, 110, 65));
    private static readonly Brush RunningFg = new SolidColorBrush(Color.FromRgb(133, 222, 128));

    private static readonly Brush StoppedBg = new SolidColorBrush(Color.FromRgb(18, 22, 28));
    private static readonly Brush StoppedBorder = new SolidColorBrush(Color.FromRgb(36, 44, 56));
    private static readonly Brush StoppedFg = new SolidColorBrush(Color.FromRgb(140, 150, 160));

    private static readonly Brush CardRunningBorder = new SolidColorBrush(Color.FromRgb(26, 68, 44));
    private static readonly Brush CardStoppedBorder = new SolidColorBrush(Color.FromRgb(28, 36, 46));

    public EngineDashboardWindow()
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);

        InitializeEngineList();
        EnginesControl.ItemsSource = _engines;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += (_, _) => RefreshStatus();
        _pollTimer.Start();

        Loaded += (_, _) => RefreshStatus();
        Closed += (_, _) => _pollTimer.Stop();
    }

    private void InitializeEngineList()
    {
        _engines.Add(new EngineItemModel
        {
            Key = "PlayoutEngine",
            Name = "KASHTRIX PLAYOUT ENGINE",
            Subtitle = "Broadcast Master Playout Engine · Program/Preview Bus",
            Description = "Primary automated 24/7 master transmission engine with hardware SDI, NDI, SCTE-35 and CG",
            ProcessName = "Kashtrix.Playout",
            IconGlyph = "▶",
            AccentBrush = new SolidColorBrush(Color.FromRgb(229, 91, 97)),
            IsInternalPlayout = true
        });

        _engines.Add(new EngineItemModel
        {
            Key = "InputEngine",
            Name = "KASHTRIX INPUT ENGINE",
            Subtitle = "Multi-Source Live Ingest & Capture Router",
            Description = "DirectShow webcams, capture cards, RTSP/RTMP/NDI feeds with dedicated live audio VU meters",
            ProcessName = "Kashtrix.InputEngine",
            IconGlyph = "🎛",
            AccentBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "OutputEngine",
            Name = "KASHTRIX OUTPUT ENGINE MASTER",
            Subtitle = "Multi-Protocol Hardware & Network Transmission Matrix",
            Description = "DeckLink 12G/3G SDI, NDI clean feeds, UDP DVB-TS, SRT, RTMP, HLS and MPD multi-channel outputs",
            ProcessName = "Kashtrix.OutputEngine",
            IconGlyph = "📡",
            AccentBrush = new SolidColorBrush(Color.FromRgb(177, 107, 228))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "CGController",
            Name = "KASHTRIX CG COMPOSITOR",
            Subtitle = "Real-Time 3D & HTML5 Broadcast Graphics Engine",
            Description = "Multi-layer lower thirds, crawlers, tickers, clocks, bugs and automated DVE graphic overlays",
            ProcessName = "Kashtrix.CGController",
            IconGlyph = "✦",
            AccentBrush = new SolidColorBrush(Color.FromRgb(229, 182, 76))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "KMix",
            Name = "KASHTRIX-MIX VIDEO SWITCHER",
            Subtitle = "16-Bus Live Production Switcher & Audio Mixer Console",
            Description = "Broadcast vision mixer with hardware T-Bar transitions, bus delegation, EQ and multi-ME control",
            ProcessName = "K-Mix",
            IconGlyph = "🎚",
            AccentBrush = new SolidColorBrush(Color.FromRgb(128, 85, 179))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "Scheduler",
            Name = "KASHTRIX SCHEDULER",
            Subtitle = "Automated Rundown Daypart & Event Timetable Engine",
            Description = "24-hour EPG, recurring playlists, commercial breaks, secondary graphic triggers and dayparts",
            ProcessName = "Kashtrix.Scheduler",
            IconGlyph = "📅",
            AccentBrush = new SolidColorBrush(Color.FromRgb(85, 200, 120))
        });
    }

    private void RefreshStatus()
    {
        var activeCount = 0;
        foreach (var engine in _engines)
        {
            if (engine.IsInternalPlayout)
            {
                var cur = Process.GetCurrentProcess();
                engine.StatusText = "RUNNING · ON AIR";
                engine.StatusBg = RunningBg;
                engine.StatusBorder = RunningBorder;
                engine.StatusFg = RunningFg;
                engine.StatusDotBrush = GreenDot;
                engine.CardBorderBrush = CardRunningBorder;
                engine.Telemetry = $"PID {cur.Id} · Memory: {cur.WorkingSet64 / (1024 * 1024):N0} MB · Threads: {cur.Threads.Count}";
                activeCount++;
                continue;
            }

            var procs = Process.GetProcessesByName(engine.ProcessName);
            if (procs.Length > 0)
            {
                var p = procs[0];
                try
                {
                    engine.StatusText = "RUNNING";
                    engine.StatusBg = RunningBg;
                    engine.StatusBorder = RunningBorder;
                    engine.StatusFg = RunningFg;
                    engine.StatusDotBrush = GreenDot;
                    engine.CardBorderBrush = CardRunningBorder;
                    engine.Telemetry = $"PID {p.Id} · Memory: {p.WorkingSet64 / (1024 * 1024):N0} MB · Threads: {p.Threads.Count}";
                    activeCount++;
                }
                catch
                {
                    engine.StatusText = "RUNNING";
                    engine.StatusBg = RunningBg;
                    engine.StatusBorder = RunningBorder;
                    engine.StatusFg = RunningFg;
                    engine.StatusDotBrush = GreenDot;
                    activeCount++;
                }
            }
            else
            {
                engine.StatusText = "STOPPED";
                engine.StatusBg = StoppedBg;
                engine.StatusBorder = StoppedBorder;
                engine.StatusFg = StoppedFg;
                engine.StatusDotBrush = GrayDot;
                engine.CardBorderBrush = CardStoppedBorder;
                engine.Telemetry = "Process inactive · Click 'START ENGINE' to launch in background";
            }
        }

        ActiveEnginesCountText.Text = $"{activeCount} / {_engines.Count} RUNNING";
        FooterStatusText.Text = $"Last refreshed at {DateTime.Now:HH:mm:ss} · All running processes synced";
    }

    private void CardStart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is EngineItemModel model)
        {
            if (model.IsInternalPlayout)
            {
                MessageBox.Show("Kashtrix Playout is already actively running as the host application.", "Playout Engine", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (StandaloneAppLauncher.Launch(model.Key, out var error))
                {
                    RefreshStatus();
                }
                else
                {
                    MessageBox.Show(error, "Start Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Start Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void CardOpenUi_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is EngineItemModel model)
        {
            if (model.IsInternalPlayout)
            {
                Application.Current.MainWindow?.Activate();
                return;
            }

            try
            {
                StandaloneAppLauncher.Launch(model.Key, out _);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Open UI", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void CardStop_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is EngineItemModel model)
        {
            if (model.IsInternalPlayout)
            {
                MessageBox.Show("Cannot terminate the primary host Playout process from this dashboard.", "Playout Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var procs = Process.GetProcessesByName(model.ProcessName);
                foreach (var p in procs)
                {
                    try { p.Kill(); } catch { }
                }
                RefreshStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not stop process: {ex.Message}", "Stop Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void StartAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var engine in _engines)
        {
            if (engine.IsInternalPlayout) continue;
            var procs = Process.GetProcessesByName(engine.ProcessName);
            if (procs.Length == 0)
            {
                try { StandaloneAppLauncher.Launch(engine.Key, out _); } catch { }
            }
        }
        RefreshStatus();
    }

    private void StopAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var engine in _engines)
        {
            if (engine.IsInternalPlayout) continue;
            try
            {
                var procs = Process.GetProcessesByName(engine.ProcessName);
                foreach (var p in procs)
                {
                    try { p.Kill(); } catch { }
                }
            }
            catch { }
        }
        RefreshStatus();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshStatus();

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2) Maximize_Click(sender, e);
        else try { DragMove(); } catch { }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        if (MaxGlyph != null) MaxGlyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
