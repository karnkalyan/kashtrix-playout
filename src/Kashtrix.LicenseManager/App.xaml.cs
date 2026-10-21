using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BroadcastPlayout.Services;

namespace Kashtrix.LicenseManager;

public partial class App : System.Windows.Application
{
    private const string AppName = "Kashtrix.LicenseManager";
    private DispatcherTimer? _heartbeatTimer;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        StandaloneAppDiagnostics.Attach(this, AppName);

        // Register session mutex and start heartbeat
        KashtrixLicenseAuthority.RegisterLicenseManagerAsRunning();
        _heartbeatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _heartbeatTimer.Tick += (_, _) => KashtrixLicenseAuthority.TouchSessionHeartbeat();
        _heartbeatTimer.Start();

        var startupSmoke = Array.Exists(e.Args, x => string.Equals(x, "--startup-smoke", StringComparison.OrdinalIgnoreCase));
        var background = Array.Exists(e.Args, x => string.Equals(x, "--background", StringComparison.OrdinalIgnoreCase) || string.Equals(x, "--daemon", StringComparison.OrdinalIgnoreCase));

        try
        {
            KashtrixLicenseAuthority.StartAutoResolveListener();

            if (background)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                StandaloneAppDiagnostics.Ready(AppName);
                return;
            }

            var window = new BroadcastPlayout.Views.LicenseManagerWindow();
            MainWindow = window;
            window.Show();
            StandaloneAppDiagnostics.Ready(AppName);
            if (startupSmoke)
            {
                var smokeTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                smokeTimer.Tick += (_, _) =>
                {
                    smokeTimer.Stop();
                    try { window.Close(); } catch { }
                    Shutdown(0);
                };
                smokeTimer.Start();
            }
        }
        catch (Exception ex)
        {
            StandaloneAppDiagnostics.Failure(AppName, ex);
            if (!startupSmoke)
            {
                try { BroadcastPlayout.Views.MessageBox.Show($"{AppName} could not start.\n\n{ex.Message}\n\nLog: {StandaloneAppDiagnostics.LogPath(AppName)}", AppName, MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
            }
            Shutdown(-1);
        }
    }
}
