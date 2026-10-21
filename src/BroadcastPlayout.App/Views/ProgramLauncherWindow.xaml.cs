using System.Windows;
using System.Windows.Input;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public partial class ProgramLauncherWindow : Window
{
    public ProgramLauncherWindow()
    {
        InitializeComponent();
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string appKey) return;

        if (string.Equals(appKey, "EngineDashboard", StringComparison.OrdinalIgnoreCase))
        {
            var win = new EngineDashboardWindow { Owner = this };
            win.Show();
            StatusText.Text = "Opened Background Engines Dashboard.";
            return;
        }

        if (string.Equals(appKey, "LicenseManager", StringComparison.OrdinalIgnoreCase))
        {
            var win = new LicenseManagerWindow { Owner = this };
            win.Show();
            StatusText.Text = "Opened Enterprise License Manager.";
            return;
        }

        if (StandaloneAppLauncher.Launch(appKey, out var error))
        {
            StatusText.Text = $"Opened {FormatName(appKey)} as a standalone application.";
            return;
        }

        StatusText.Text = error;
        MessageBox.Show(error, "Kashtrix Program Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string FormatName(string appKey) => appKey switch
    {
        "Playout" or "Playout.app" or "Kashtrix.Playout" => "Kashtrix Playout Master Engine",
        "OutputEngine" or "Kashtrix.OutputEngine" => "Kashtrix Output Engine Master",
        "InputEngine" or "Kashtrix.InputEngine" => "Kashtrix Input Engine",
        "CGEditor" or "Kashtrix.CGEditor" => "Kashtrix CG Designer",
        "CGController" or "Kashtrix.CGController" => "Kashtrix CG Controller",
        "PlaylistEditor" or "Kashtrix.PlaylistEditor" => "Kashtrix Playlist Editor",
        "Scheduler" or "Kashtrix.Scheduler" or "PlaylistScheduler" or "Kashtrix.PlaylistScheduler" => "Kashtrix Broadcast Scheduler",
        "ChannelController" or "Kashtrix.ChannelController" => "Kashtrix Channel Controller",
        "FileManager" or "Kashtrix.FileManager" => "Kashtrix File Manager",
        "QCController" or "Kashtrix.QCController" => "Kashtrix QC Controller",
        "IngestServer" or "Kashtrix.IngestServer" or "Ingest" or "Kashtrix.Ingest" => "Kashtrix Ingest Server",
        "MAM" or "Kashtrix.MAM" => "Kashtrix Media Asset Management",
        "NRCS" or "Kashtrix.NRCS" => "Kashtrix NRCS Newsroom",
        "Prompter" or "Kashtrix.Prompter" => "Kashtrix Teleprompter",
        "HAController" or "Kashtrix.HAController" => "Kashtrix HA / Redundancy Controller",
        "ApiGateway" or "Kashtrix.ApiGateway" or "API Gateway" or "Kashtrix.API Gateway" => "Kashtrix API Gateway / Web MCR",
        "Multiview" or "Kashtrix.Multiview" or "Multiviewer" or "Kashtrix.Multiviewer" => "Kashtrix Multiviewer",
        "Settings" or "Kashtrix.Settings" => "Kashtrix Settings",
        "EngineDashboard" => "Background Engines Dashboard",
        "LicenseManager" => "License Manager",
        _ => appKey
    };

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowChromeActions.Drag(this, e);
}
