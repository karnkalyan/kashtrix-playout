using System;
using System.IO;
using System.Windows;
using BroadcastPlayout.Services;

namespace Kashtrix.InputEngine;

public partial class App : System.Windows.Application
{
    private const string AppName = "Kashtrix.InputEngine";

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        StandaloneAppDiagnostics.Attach(this, AppName);
        var startupSmoke = Array.Exists(e.Args, x => string.Equals(x, "--startup-smoke", StringComparison.OrdinalIgnoreCase));
        try
        {
            try { BroadcastPlayout.Ffmpeg.FfmpegRuntime.Initialize(Path.Combine(AppContext.BaseDirectory, "native", "ffmpeg")); } catch { }

            var window = new MainWindow();
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
            Shutdown(-1);
        }
    }
}
