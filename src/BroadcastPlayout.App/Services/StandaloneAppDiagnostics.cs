using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BroadcastPlayout.Services;

public static class StandaloneAppDiagnostics
{
    private static int _globalHandlersRegistered;
    private static System.Threading.Timer? _heartbeatTimer;
    private static System.Windows.Forms.NotifyIcon? _serviceTrayIcon;
    private static bool _isExplicitServiceShutdown;
    private static string? _activeAppName;

    public static string LogPath(string appName)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KashtrixPlayout", "Logs");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, appName.Replace(' ', '_') + ".startup.log");
    }

    public static void Attach(Application app, string appName)
    {
        _activeAppName = appName;
        RuntimeDiagnosticsService.Initialize(appName);
        _heartbeatTimer ??= new System.Threading.Timer(_ => RuntimeDiagnosticsService.WriteProcessHeartbeat("APP_RUNNING"), null, 2000, 5000);
        Write(appName, "START", null);

        var smoke = Environment.GetCommandLineArgs().Any(x => string.Equals(x, "--startup-smoke", StringComparison.OrdinalIgnoreCase));
        if (!smoke && !KashtrixLicenseAuthority.VerifyApplicationStartup(appName, out var licenseReason))
        {
            Write(appName, "LICENSE GATE BLOCKED: " + licenseReason, null);
            try
            {
                BroadcastPlayout.Views.LicenseActivationPromptWindow.ShowPrompt(appName, licenseReason);
            }
            catch
            {
                try
                {
                    BroadcastPlayout.Views.MessageBox.Show(
                        $"[Kashtrix License Authority]\n\n{licenseReason}\n\nConnection and active license verified by Kashtrix License Manager are required.",
                        appName, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch { }
            }
            Environment.ExitCode = -2;
            try { app.Shutdown(-2); } catch { }
            return;
        }

        // Initialize background service tray icon
        if (!smoke)
        {
            app.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                try
                {
                    InitializeServiceTray(app, appName);
                }
                catch { }
            });
        }

        if (Interlocked.Exchange(ref _globalHandlersRegistered, 1) == 0)
        {
            // Hook Loaded event for all windows
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) =>
            {
                if (sender is Window window)
                {
                    if (!KashtrixLicenseAuthority.IsActivated)
                    {
                        if (!string.IsNullOrEmpty(window.Title) && !window.Title.Contains("[UNLICENSED]", StringComparison.OrdinalIgnoreCase))
                        {
                            window.Title = $"{window.Title} [UNLICENSED]";
                        }
                    }

                    // Attach background service minimize-to-tray on window close
                    window.Closing -= HandleWindowClosing;
                    window.Closing += HandleWindowClosing;
                }
            }));

            // Escape key handling
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.PreviewKeyDownEvent, new KeyEventHandler((sender, e) =>
            {
                if (e.Handled || e.Key != Key.Escape || sender is not Window window || !window.IsActive) return;
                var name = window.GetType().Name;
                if (name is "MainWindow" or "SettingsWindow" or "MediaAssetManagementWindow" or "IngestServerWindow"
                    or "FrameComparisonWindow" or "MultiviewWindow" or "CgEditorWindow" or "CgControllerWindow"
                    or "SchedulerWindow" or "ChannelManagerWindow" or "ChannelControllerWindow" or "AuditLogWindow"
                    or "VideoProcessorWindow" or "InputSourceWindow" or "AudioMixerWindow" or "QcControllerWindow"
                    or "FileManagerWindow" or "PlaylistEditorWindow" or "UserManagementWindow" or "EpgOutputSettingsWindow" or "BroadcastTimersClockWindow")
                    return;
                try { window.Close(); e.Handled = true; } catch { }
            }));
        }

        app.DispatcherUnhandledException += (_, e) =>
        {
            Write(appName, "DISPATCHER UNHANDLED", e.Exception);
            var isSmoke = Environment.GetCommandLineArgs().Any(x => string.Equals(x, "--startup-smoke", StringComparison.OrdinalIgnoreCase));
            if (isSmoke)
            {
                e.Handled = true;
                Environment.ExitCode = -1;
                try { app.Shutdown(-1); } catch { }
                return;
            }
            try
            {
                var details = e.Exception.Message;
                var inner = e.Exception.InnerException;
                if (inner is not null) details += "\n\nCause: " + inner.Message;
                if (inner?.InnerException is not null) details += "\n\nRoot cause: " + inner.InnerException.Message;
                BroadcastPlayout.Views.MessageBox.Show($"{appName} encountered an error.\n\n{details}\n\nLog: {LogPath(appName)}", appName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Write(appName, "APPDOMAIN UNHANDLED", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Write(appName, "TASK UNOBSERVED", e.Exception);
    }

    private static void HandleWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var isSmoke = Environment.GetCommandLineArgs().Any(x => string.Equals(x, "--startup-smoke", StringComparison.OrdinalIgnoreCase));
        if (isSmoke || _isExplicitServiceShutdown) return;

        if (sender is Window window)
        {
            // Allow child dialogs with owner to close normally
            if (window.Owner != null) return;

            // Standalone root application window: minimize to system notification tray!
            e.Cancel = true;
            window.Hide();
            try
            {
                _serviceTrayIcon?.ShowBalloonTip(
                    2000,
                    _activeAppName ?? "Kashtrix Broadcast Service",
                    $"{_activeAppName ?? "Application"} is running continuously in the background.\nTo terminate, right-click this icon in the notification tray and select 'Exit & Shut Down Service'.",
                    System.Windows.Forms.ToolTipIcon.Info);
            }
            catch { }
        }
    }

    private static void InitializeServiceTray(Application app, string appName)
    {
        if (_serviceTrayIcon != null) return;

        var tray = new System.Windows.Forms.NotifyIcon();
        var icon = ResolveTrayIcon(appName);
        tray.Icon = icon ?? System.Drawing.SystemIcons.Application;

        var title = $"{appName} · Service Running";
        if (title.Length > 63) title = title[..63];
        tray.Text = title;
        tray.Visible = true;

        var menu = new System.Windows.Forms.ContextMenuStrip();

        var headerItem = new System.Windows.Forms.ToolStripMenuItem($"Kashtrix Broadcast Service: {appName}")
        {
            Enabled = false
        };
        menu.Items.Add(headerItem);

        var openItem = new System.Windows.Forms.ToolStripMenuItem($"Open {appName} Console")
        {
            Font = new System.Drawing.Font(menu.Font, System.Drawing.FontStyle.Bold)
        };
        openItem.Click += (_, _) => RestoreApplication(app);
        menu.Items.Add(openItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit & Shut Down Service");
        exitItem.Click += (_, _) =>
        {
            _isExplicitServiceShutdown = true;
            try { tray.Visible = false; tray.Dispose(); } catch { }
            try { app.Shutdown(0); } catch { }
            try { Environment.Exit(0); } catch { }
        };
        menu.Items.Add(exitItem);

        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => RestoreApplication(app);
        tray.Click += (_, e) =>
        {
            if (e is System.Windows.Forms.MouseEventArgs me && me.Button == System.Windows.Forms.MouseButtons.Left)
            {
                RestoreApplication(app);
            }
        };

        _serviceTrayIcon = tray;
    }

    private static void RestoreApplication(Application app)
    {
        app.Dispatcher.Invoke(() =>
        {
            foreach (Window w in app.Windows)
            {
                if (w.Owner == null)
                {
                    w.Show();
                    if (w.WindowState == WindowState.Minimized)
                    {
                        w.WindowState = WindowState.Normal;
                    }
                    w.Activate();
                    w.Focus();
                    return;
                }
            }
        });
    }

    private static System.Drawing.Icon? ResolveTrayIcon(string appName)
    {
        try
        {
            var proc = Process.GetCurrentProcess();
            var mainMod = proc.MainModule?.FileName;
            if (!string.IsNullOrEmpty(mainMod) && File.Exists(mainMod))
            {
                var extracted = System.Drawing.Icon.ExtractAssociatedIcon(mainMod);
                if (extracted != null) return extracted;
            }
        }
        catch { }

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var brandingDir = Path.Combine(baseDir, "Branding");
            var shortName = appName.ToLowerInvariant().Replace("kashtrix.", "kashtrix-").Replace(" ", "").Replace(".", "-");
            var possible = Path.Combine(brandingDir, $"{shortName}.ico");
            if (File.Exists(possible)) return new System.Drawing.Icon(possible);

            var playoutIco = Path.Combine(brandingDir, "kashtrix-playout.ico");
            if (File.Exists(playoutIco)) return new System.Drawing.Icon(playoutIco);
        }
        catch { }

        return System.Drawing.SystemIcons.Application;
    }

    public static void Ready(string appName) { Write(appName, "WINDOW READY", null); RuntimeDiagnosticsService.Write("WINDOW_READY", appName); }
    public static void Failure(string appName, Exception ex) { Write(appName, "STARTUP FAILED", ex); RuntimeDiagnosticsService.WriteException("STARTUP_FAILED", ex, appName); }

    private static void Write(string appName, string state, Exception? ex)
    {
        try
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(" | ").Append(state);
            if (ex is not null) sb.AppendLine().Append(ex);
            sb.AppendLine();
            File.AppendAllText(LogPath(appName), sb.ToString(), new UTF8Encoding(false));
            if (ex is null) RuntimeDiagnosticsService.Write("APP_STATE", state, new { appName });
            else RuntimeDiagnosticsService.WriteException(state.Replace(' ', '_'), ex, appName);
        }
        catch { }
    }
}

