using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BroadcastPlayout.Services;

namespace Kashtrix.ApiGateway;

public partial class MainWindow : Window
{
    private readonly GatewayHost _host = new();
    private readonly DispatcherTimer _pollTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _logCount;

    public MainWindow()
    {
        InitializeComponent();
        _host.Log += OnGatewayLog;
        RefreshLabels();
        RefreshChannels();
        RefreshClients();

        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");

        _pollTimer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            RefreshChannels();
            RefreshClients();
        };
        _pollTimer.Start();

        try
        {
            _host.Start();
            UpdateStatus(true);
        }
        catch (Exception ex)
        {
            LogBox.Text = $"[{DateTime.Now:HH:mm:ss}] [ERROR] Startup failed: {ex.Message}{Environment.NewLine}";
            UpdateStatus(false);
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Maximize_Click(sender, e);
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void OnGatewayLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            _logCount++;
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogBox.AppendText(line + Environment.NewLine);
            LogCountText.Text = $" ({_logCount} events)";
            if (AutoScrollCheck.IsChecked == true)
            {
                LogBox.ScrollToEnd();
            }
        });
    }

    private void RefreshLabels()
    {
        var s = _host.Settings;
        HttpText.Text = $"http://{s.BindHost}:{s.HttpPort}";
        TcpText.Text = $"{s.BindHost}:{s.TcpPort}";
        UdpText.Text = $"{s.BindHost}:{s.UdpPort}";
        McpText.Text = $"http://{s.BindHost}:{s.HttpPort}/mcp";
    }

    private void RefreshChannels()
    {
        try
        {
            var channels = PlatformControlBus.ReadStatuses();
            ChannelsList.ItemsSource = channels;
        }
        catch { }
    }

    private void RefreshClients()
    {
        try
        {
            var clients = _host.GetConnectedClients();
            ClientsList.ItemsSource = clients;
            var activeCount = clients.Count(c => c.Status != "Closed");
            ClientsCountText.Text = $" ({activeCount} active / {clients.Count} total)";
        }
        catch { }
    }

    private void RefreshChannels_Click(object sender, RoutedEventArgs e) => RefreshChannels();
    private void RefreshClients_Click(object sender, RoutedEventArgs e) => RefreshClients();

    private void UpdateStatus(bool online)
    {
        if (online)
        {
            StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x33, 0x22));
            StatusBadge.BorderBrush = (SolidColorBrush)FindResource("GoodBrush");
            StatusBadgeText.Foreground = (SolidColorBrush)FindResource("GoodBrush");
            StatusBadgeText.Text = $"ONLINE · {_host.Settings.BindHost}:{_host.Settings.HttpPort}";
        }
        else
        {
            StatusBadge.Background = new SolidColorBrush(Color.FromRgb(0x3B, 0x14, 0x16));
            StatusBadge.BorderBrush = (SolidColorBrush)FindResource("OnAirBrush");
            StatusBadgeText.Foreground = (SolidColorBrush)FindResource("OnAirBrush");
            StatusBadgeText.Text = "OFFLINE · STOPPED";
        }
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _host.Start();
            UpdateStatus(true);
            OnGatewayLog("[GATEWAY] Started successfully.");
        }
        catch (Exception ex)
        {
            OnGatewayLog($"[ERROR] Start failed: {ex.Message}");
            UpdateStatus(false);
        }
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _host.Stop();
        UpdateStatus(false);
    }

    private void OpenWeb_Click(object sender, RoutedEventArgs e)
    {
        var s = _host.Settings;
        LaunchUrl($"http://{s.BindHost}:{s.HttpPort}/");
    }

    private void OpenPlayoutWeb_Click(object sender, RoutedEventArgs e)
    {
        LaunchUrl("http://127.0.0.1:5173/");
    }

    private void OpenCgWeb_Click(object sender, RoutedEventArgs e)
    {
        LaunchUrl("http://127.0.0.1:5174/");
    }

    private static void LaunchUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear();
        _logCount = 0;
        LogCountText.Text = " (0 events)";
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _pollTimer.Stop();
        _host.Dispose();
    }
}
