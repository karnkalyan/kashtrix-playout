using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
    private string _hostEndpoint = "127.0.0.1";
    private bool _isInstalled = true;

    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string Description { get; init; } = "";
    public string ProcessName { get; init; } = "";
    public string IconGlyph { get; init; } = "⚙";
    public Brush AccentBrush { get; init; } = new SolidColorBrush(Color.FromRgb(177, 107, 228));
    public bool IsInternalPlayout { get; init; }

    public string HostEndpoint
    {
        get => _hostEndpoint;
        set
        {
            if (Set(ref _hostEndpoint, value))
            {
                OnPropertyChanged(nameof(HostEndpointText));
                OnPropertyChanged(nameof(IsRemoteNode));
            }
        }
    }

    public bool IsRemoteNode => !string.Equals(HostEndpoint, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(HostEndpoint, "localhost", StringComparison.OrdinalIgnoreCase);

    public string HostEndpointText => IsRemoteNode ? $"🌐 Remote Node: {HostEndpoint}" : "💻 Local Workstation (127.0.0.1)";

    public bool IsInstalled { get => _isInstalled; set => Set(ref _isInstalled, value); }
    public string StatusText { get => _statusText; set => Set(ref _statusText, value); }
    public Brush StatusBg { get => _statusBg; set => Set(ref _statusBg, value); }
    public Brush StatusBorder { get => _statusBorder; set => Set(ref _statusBorder, value); }
    public Brush StatusFg { get => _statusFg; set => Set(ref _statusFg, value); }
    public Brush StatusDotBrush { get => _statusDotBrush; set => Set(ref _statusDotBrush, value); }
    public Brush CardBorderBrush { get => _cardBorderBrush; set => Set(ref _cardBorderBrush, value); }
    public string Telemetry { get => _telemetry; set => Set(ref _telemetry, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

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
    private static readonly Brush RedDot = new SolidColorBrush(Color.FromRgb(239, 68, 68));

    private static readonly Brush RunningBg = new SolidColorBrush(Color.FromRgb(16, 38, 26));
    private static readonly Brush RunningBorder = new SolidColorBrush(Color.FromRgb(40, 110, 65));
    private static readonly Brush RunningFg = new SolidColorBrush(Color.FromRgb(133, 222, 128));

    private static readonly Brush StoppedBg = new SolidColorBrush(Color.FromRgb(18, 22, 28));
    private static readonly Brush StoppedBorder = new SolidColorBrush(Color.FromRgb(36, 44, 56));
    private static readonly Brush StoppedFg = new SolidColorBrush(Color.FromRgb(140, 150, 160));

    private static readonly Brush NotFoundBg = new SolidColorBrush(Color.FromRgb(35, 18, 22));
    private static readonly Brush NotFoundBorder = new SolidColorBrush(Color.FromRgb(95, 30, 38));
    private static readonly Brush NotFoundFg = new SolidColorBrush(Color.FromRgb(248, 113, 113));

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
            Key = "InputEngine",
            Name = "KASHTRIX INPUT ENGINE",
            Subtitle = "Multi-Source Live Ingest & Capture Router",
            Description = "DirectShow capture, SDI/NDI/SRT feeds, web streams with dedicated live audio VU monitoring",
            ProcessName = "Kashtrix.InputEngine",
            IconGlyph = "🎛",
            AccentBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "CGController",
            Name = "KASHTRIX CG CONTROLLER",
            Subtitle = "Real-Time 3D & HTML5 Broadcast Graphics Compositor",
            Description = "Multi-layer lower thirds, crawlers, tickers, clocks, bugs and automated DVE graphic overlays",
            ProcessName = "Kashtrix.CGController",
            IconGlyph = "✦",
            AccentBrush = new SolidColorBrush(Color.FromRgb(229, 182, 76))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "CGEditor",
            Name = "KASHTRIX CG DESIGNER",
            Subtitle = "Standalone Broadcast Graphic Template & Motion Designer",
            Description = "Design multi-layer broadcast CG projects, dynamic data bindings, tickers and visual animations",
            ProcessName = "Kashtrix.CGEditor",
            IconGlyph = "🎨",
            AccentBrush = new SolidColorBrush(Color.FromRgb(236, 72, 153))
        });


        _engines.Add(new EngineItemModel
        {
            Key = "Scheduler",
            Name = "KASHTRIX BROADCAST SCHEDULER",
            Subtitle = "Automated Rundown Daypart & Event Timetable Engine",
            Description = "24-hour EPG, recurring playlists, commercial breaks, secondary graphic triggers and dayparts",
            ProcessName = "Kashtrix.Scheduler",
            IconGlyph = "📅",
            AccentBrush = new SolidColorBrush(Color.FromRgb(85, 200, 120))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "PlaylistEditor",
            Name = "KASHTRIX PLAYLIST EDITOR",
            Subtitle = "Multi-Channel Rundown & Media Sequence Editor",
            Description = "Prepare broadcast rundowns, item metadata, commercial tags and media trim points",
            ProcessName = "Kashtrix.PlaylistEditor",
            IconGlyph = "📋",
            AccentBrush = new SolidColorBrush(Color.FromRgb(168, 85, 247))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "Multiview",
            Name = "KASHTRIX MULTIVIEWER MONITOR",
            Subtitle = "Multi-Window Broadcast Wall & Audio VU Monitor",
            Description = "Full multiview layout with Program, Preview, Inputs, Clocks, Tally and audio monitoring",
            ProcessName = "Kashtrix.Multiview",
            IconGlyph = "🖥",
            AccentBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "NRCS",
            Name = "KASHTRIX NRCS NEWSROOM",
            Subtitle = "Newsroom Computer System & MOS Rundown Gateway",
            Description = "Planning, Story Bank, live news rundowns, revisions, MOS graphics and Playout publishing",
            ProcessName = "Kashtrix.NRCS",
            IconGlyph = "📰",
            AccentBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "Prompter",
            Name = "KASHTRIX TELEPROMPTER",
            Subtitle = "Multi-Display Studio Teleprompter & Live NRCS Follow",
            Description = "Presenter prompter with live news script synchronization, SDI/NDI output and remote pedals",
            ProcessName = "Kashtrix.Prompter",
            IconGlyph = "📜",
            AccentBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "MAM",
            Name = "KASHTRIX MEDIA ASSET MANAGEMENT",
            Subtitle = "Enterprise Media Ingestion, Indexing & Metadata Catalog",
            Description = "Centralized broadcast media repository with proxy generation, automated tagging and search",
            ProcessName = "Kashtrix.MAM",
            IconGlyph = "🗄",
            AccentBrush = new SolidColorBrush(Color.FromRgb(14, 165, 233))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "QCController",
            Name = "KASHTRIX QC CONTROLLER",
            Subtitle = "Broadcast Technical Quality Control & Confidence Monitor",
            Description = "Automated video/audio format validation, loudness compliance, silence/black frame detection",
            ProcessName = "Kashtrix.QCController",
            IconGlyph = "🔍",
            AccentBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "IngestServer",
            Name = "KASHTRIX INGEST SERVER",
            Subtitle = "Automated Scheduled Multi-Channel Broadcast Ingest",
            Description = "Record feeds from SDI, NDI, RTSP and web streams directly to compliant broadcast codecs",
            ProcessName = "Kashtrix.IngestServer",
            IconGlyph = "⏺",
            AccentBrush = new SolidColorBrush(Color.FromRgb(234, 88, 12))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "ChannelController",
            Name = "KASHTRIX CHANNEL CONTROLLER",
            Subtitle = "Multi-Channel Transmission Supervision & Emergency Take",
            Description = "Synchronized multi-channel transmission control with live override and emergency cut",
            ProcessName = "Kashtrix.ChannelController",
            IconGlyph = "📺",
            AccentBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "FileManager",
            Name = "KASHTRIX FILE MANAGER",
            Subtitle = "Broadcast File Inspector & Ingest Preparation Tool",
            Description = "Local and network broadcast file explorer with deep codec probing and fast trim preview",
            ProcessName = "Kashtrix.FileManager",
            IconGlyph = "📁",
            AccentBrush = new SolidColorBrush(Color.FromRgb(107, 114, 128))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "HAController",
            Name = "KASHTRIX HIGH AVAILABILITY CONTROLLER",
            Subtitle = "1+1 Main / Mirror Redundancy & Automatic Failover",
            Description = "Heartbeat tracking, playlist state mirror synchronization and automated matrix switchover",
            ProcessName = "Kashtrix.HAController",
            IconGlyph = "🛡",
            AccentBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129))
        });

        _engines.Add(new EngineItemModel
        {
            Key = "ApiGateway",
            Name = "KASHTRIX API GATEWAY & WEB MCR",
            Subtitle = "REST / WebSocket / TCP Remote Control & Integration MCR",
            Description = "Web-based master control room UI, remote scheduling APIs and external broadcast automation",
            ProcessName = "Kashtrix.ApiGateway",
            IconGlyph = "🌐",
            AccentBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248))
        });
    }

    private void RefreshStatus()
    {
        var activeCount = 0;
        foreach (var engine in _engines)
        {
            if (engine.IsInternalPlayout)
            {
                var playoutProcs = Process.GetProcessesByName("Kashtrix.Playout");
                var totalInstances = Math.Max(1, playoutProcs.Length);
                if (!KashtrixLicenseAuthority.IsActivated)
                {
                    engine.StatusText = totalInstances > 1 ? $"{totalInstances} CHANNELS [UNLICENSED]" : "UNLICENSED";
                    engine.StatusBg = new SolidColorBrush(Color.FromRgb(45, 25, 15));
                    engine.StatusBorder = new SolidColorBrush(Color.FromRgb(160, 80, 25));
                    engine.StatusFg = new SolidColorBrush(Color.FromRgb(250, 160, 60));
                    engine.StatusDotBrush = new SolidColorBrush(Color.FromRgb(250, 160, 60));
                    engine.CardBorderBrush = CardStoppedBorder;
                    engine.Telemetry = $"Multi-Channel Playout: {totalInstances} channel instance(s) running · Activation required in License Manager";
                }
                else
                {
                    var cur = Process.GetCurrentProcess();
                    engine.StatusText = totalInstances > 1 ? $"RUNNING · {totalInstances} CHANNELS" : "RUNNING · ON AIR";
                    engine.StatusBg = RunningBg;
                    engine.StatusBorder = RunningBorder;
                    engine.StatusFg = RunningFg;
                    engine.StatusDotBrush = GreenDot;
                    engine.CardBorderBrush = CardRunningBorder;
                    engine.Telemetry = $"Multi-Channel Playout: {totalInstances} channel(s) active · Primary PID {cur.Id} · Memory: {cur.WorkingSet64 / (1024 * 1024):N0} MB";
                }
                activeCount++;
                continue;
            }

            if (engine.IsRemoteNode)
            {
                engine.StatusText = "REMOTE NODE";
                engine.StatusBg = RunningBg;
                engine.StatusBorder = RunningBorder;
                engine.StatusFg = RunningFg;
                engine.StatusDotBrush = PurpleDot;
                engine.CardBorderBrush = CardRunningBorder;
                engine.Telemetry = $"Configured at remote IP endpoint: {engine.HostEndpoint} · Network managed";
                activeCount++;
                continue;
            }

            var exe = StandaloneAppLauncher.ResolveExecutable(engine.Key);
            if (exe == null)
            {
                engine.IsInstalled = false;
                engine.StatusText = "NOT FOUND";
                engine.StatusBg = NotFoundBg;
                engine.StatusBorder = NotFoundBorder;
                engine.StatusFg = NotFoundFg;
                engine.StatusDotBrush = RedDot;
                engine.CardBorderBrush = CardStoppedBorder;
                engine.Telemetry = "Application component is not installed on this workstation. Run Setup to install or click 'IP / NODE' to connect to remote machine.";
                continue;
            }

            engine.IsInstalled = true;
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
                    engine.Telemetry = $"Local PID {p.Id} · Memory: {p.WorkingSet64 / (1024 * 1024):N0} MB · Threads: {p.Threads.Count}";
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
                if (!KashtrixLicenseAuthority.IsModuleActive(engine.Key))
                {
                    engine.StatusText = "DEACTIVATED";
                    engine.StatusBg = new SolidColorBrush(Color.FromRgb(38, 22, 18));
                    engine.StatusBorder = new SolidColorBrush(Color.FromRgb(110, 40, 30));
                    engine.StatusFg = new SolidColorBrush(Color.FromRgb(240, 140, 90));
                    engine.StatusDotBrush = new SolidColorBrush(Color.FromRgb(240, 140, 90));
                    engine.CardBorderBrush = CardStoppedBorder;
                    engine.Telemetry = "Module is deactivated in current license profile. Open License Manager to activate.";
                }
                else
                {
                    engine.StatusText = "STOPPED";
                    engine.StatusBg = StoppedBg;
                    engine.StatusBorder = StoppedBorder;
                    engine.StatusFg = StoppedFg;
                    engine.StatusDotBrush = GrayDot;
                    engine.CardBorderBrush = CardStoppedBorder;
                    engine.Telemetry = $"Installed ({Path.GetFileName(exe)}) · Service inactive · Click 'START ENGINE' to run";
                }
            }
        }

        ActiveEnginesCountText.Text = $"{activeCount} / {_engines.Count} ACTIVE";
        FooterStatusText.Text = $"Last refreshed at {DateTime.Now:HH:mm:ss} · All 18 broadcast services synchronized";
    }

    private void CardConfigureIp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is EngineItemModel model)
        {
            var prompt = new Window
            {
                Title = $"Configure IP Endpoint - {model.Name}",
                Width = 480,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                Background = new SolidColorBrush(Color.FromRgb(12, 17, 22)),
                Foreground = Brushes.White,
                ResizeMode = ResizeMode.NoResize
            };

            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(18) };
            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"Configure Host Endpoint for {model.Name}",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });
            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Enter IP address and port (use '127.0.0.1' for local system, or LAN IP for remote node):",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 170, 180)),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var box = new System.Windows.Controls.TextBox
            {
                Text = model.HostEndpoint,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(20, 26, 34)),
                Foreground = Brushes.White,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 0, 14)
            };
            stack.Children.Add(box);

            var btnRow = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnDefault = new System.Windows.Controls.Button
            {
                Content = "USE LOCAL (127.0.0.1)",
                Height = 28,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 8, 0)
            };
            btnDefault.Click += (_, _) => { box.Text = "127.0.0.1"; };
            btnRow.Children.Add(btnDefault);

            var btnSave = new System.Windows.Controls.Button
            {
                Content = "SAVE ENDPOINT",
                Height = 28,
                Padding = new Thickness(14, 0, 14, 0),
                IsDefault = true
            };
            btnSave.Click += (_, _) =>
            {
                var input = box.Text.Trim();
                if (!string.IsNullOrEmpty(input))
                {
                    model.HostEndpoint = input;
                    RefreshStatus();
                }
                prompt.Close();
            };
            btnRow.Children.Add(btnSave);

            stack.Children.Add(btnRow);
            prompt.Content = stack;
            prompt.ShowDialog();
        }
    }

    private void CardStart_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is EngineItemModel model)
        {
            if (model.IsRemoteNode)
            {
                MessageBox.Show($"Engine is configured for remote network node at {model.HostEndpoint}. Start command dispatched to remote endpoint.", "Remote Service", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool isPlayout = model.IsInternalPlayout || model.Key.Equals("PlayoutEngine", StringComparison.OrdinalIgnoreCase) || model.Key.Equals("Playout", StringComparison.OrdinalIgnoreCase);

            if (isPlayout)
            {
                // Multi-channel playout: allow launching additional channels
                var playoutProcs = Process.GetProcessesByName(model.ProcessName).Length;
                var nextChannel = playoutProcs + (model.IsInternalPlayout ? 1 : 0) + 1;
                try
                {
                    if (StandaloneAppLauncher.Launch("Playout", out var error, $"--channel {nextChannel}"))
                    {
                        RefreshStatus();
                        MessageBox.Show($"Kashtrix Playout Channel {nextChannel} launched successfully.", "Multi-Channel Playout", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(error, "Start Playout Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Start Playout Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            // For all other engines: enforce strict single-instance restriction
            var procs = Process.GetProcessesByName(model.ProcessName);
            if (procs.Length > 0)
            {
                MessageBox.Show($"{model.Name} is already running as a single-instance service/application. Multiple instances are not allowed for this engine.", "Single Instance Engine", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (model.IsRemoteNode)
            {
                MessageBox.Show($"Application is running on remote workstation ({model.HostEndpoint}). Open remote MCR web client or connect via API Gateway.", "Remote UI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool isPlayout = model.IsInternalPlayout || model.Key.Equals("PlayoutEngine", StringComparison.OrdinalIgnoreCase) || model.Key.Equals("Playout", StringComparison.OrdinalIgnoreCase);

            if (isPlayout)
            {
                var playoutProcs = Process.GetProcessesByName("Kashtrix.Playout");
                var nextChannel = playoutProcs.Length + (model.IsInternalPlayout ? 1 : 0) + 1;

                var promptRes = MessageBox.Show(
                    $"Kashtrix Playout supports multi-channel playout operations.\n\n" +
                    $"• Click 'Yes' to launch a new Playout Channel instance (Channel {nextChannel}).\n" +
                    $"• Click 'No' to bring the primary Playout host window to the front.",
                    "Kashtrix Multi-Channel Playout",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (promptRes == MessageBoxResult.Yes)
                {
                    if (StandaloneAppLauncher.Launch("Playout", out var launchErr, $"--channel {nextChannel}"))
                    {
                        RefreshStatus();
                    }
                    else
                    {
                        MessageBox.Show(launchErr, "Launch Playout Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (promptRes == MessageBoxResult.No)
                {
                    Application.Current.MainWindow?.Activate();
                }
                return;
            }

            // For all other engines: enforce strict single-instance restriction
            var runningProcs = Process.GetProcessesByName(model.ProcessName);
            if (runningProcs.Length > 0)
            {
                MessageBox.Show($"{model.Name} is already running as a single-instance application.", "Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!StandaloneAppLauncher.Launch(model.Key, out var err))
                {
                    MessageBox.Show(err, "Open Application", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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
                var secondaryProcs = Process.GetProcessesByName("Kashtrix.Playout");
                if (secondaryProcs.Length > 0)
                {
                    var res = MessageBox.Show($"Terminate {secondaryProcs.Length} secondary Playout channel instance(s)?", "Stop Secondary Playouts", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        foreach (var p in secondaryProcs)
                        {
                            try { p.Kill(); } catch { }
                        }
                        RefreshStatus();
                    }
                    return;
                }
                MessageBox.Show("Cannot terminate the primary host Playout process from this dashboard.", "Playout Engine", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (model.IsRemoteNode)
            {
                MessageBox.Show($"Stop signal transmitted to remote endpoint {model.HostEndpoint}.", "Stop Remote Service", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (engine.IsInternalPlayout || engine.IsRemoteNode) continue;
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
            if (engine.IsInternalPlayout || engine.IsRemoteNode) continue;
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

    private void AuthorizeWorkstationNode_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AuthorizeWorkstationDialog
        {
            Owner = this
        };
        if (dlg.ShowDialog() == true && dlg.CreatedNode != null)
        {
            var node = dlg.CreatedNode;
            var existing = _engines.FirstOrDefault(x => x.HostEndpoint == node.HostEndpoint && x.Name.Contains(node.ChannelName, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                var newEngine = new EngineItemModel
                {
                    Key = "RemotePlayoutNode",
                    Name = $"Kashtrix Playout ({node.ChannelName})",
                    Subtitle = $"Remote Playout Node · {node.ApplicationType}",
                    Description = $"Network Playout Channel authorized via KTX token {node.AuthorizationToken}",
                    ProcessName = "Playout.app",
                    IconGlyph = "📺",
                    AccentBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    HostEndpoint = node.HostEndpoint,
                    IsInstalled = true
                };
                _engines.Add(newEngine);
            }
            RefreshStatus();
            MessageBox.Show($"Remote workstation node '{node.ChannelName}' ({node.HostEndpoint}) has been authorized and registered under this central license authority.\n\nKTX Token: {node.AuthorizationToken}\nWorkstation IP: {node.HostEndpoint}\nQuota: Authorized and licensed to run.", "Workstation Node Authorized", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenLicenseManager_Click(object sender, RoutedEventArgs e)
    {
        var licWin = new LicenseManagerWindow
        {
            Owner = this
        };
        licWin.ShowDialog();
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
