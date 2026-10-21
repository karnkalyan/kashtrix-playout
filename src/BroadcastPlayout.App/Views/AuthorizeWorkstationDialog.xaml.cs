using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public partial class AuthorizeWorkstationDialog : Window
{
    public AuthorizedWorkstationNode? CreatedNode { get; private set; }

    public AuthorizeWorkstationDialog()
    {
        InitializeComponent();
        UpdateQuotaSummary();
        AppTypeCombo.SelectionChanged += (_, _) => UpdateQuotaSummary();
    }

    private void UpdateQuotaSummary()
    {
        try
        {
            var p = KashtrixLicenseAuthority.Current;
            var activePlayouts = p.AuthorizedNodes.Count(n => n.IsAuthorized && n.ApplicationType.Contains("Playout", StringComparison.OrdinalIgnoreCase));
            QuotaSummaryText.Text = $"Playout Quota: {activePlayouts} / {p.PlayoutMaxChannels} Channels Authorized · Max Resolution: {p.PlayoutMaxResolution}";
        }
        catch { }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Authorize_Click(object sender, RoutedEventArgs e)
    {
        var appItem = AppTypeCombo.SelectedItem as ComboBoxItem;
        var appType = appItem?.Content?.ToString() ?? "Playout.app";
        var channelName = ChannelNameBox.Text.Trim();
        var host = HostIpBox.Text.Trim();
        var hwid = HwidBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(channelName))
        {
            ValidationStatusText.Text = "Please enter a channel or node name.";
            ChannelNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            ValidationStatusText.Text = "Please enter workstation IP address or hostname.";
            HostIpBox.Focus();
            return;
        }

        var (success, message, node) = KashtrixLicenseAuthority.RegisterWorkstationNode(channelName, host, appType, hwid);
        if (!success)
        {
            ValidationStatusText.Text = message;
            MessageBox.Show(message, "License Quota Limit", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreatedNode = node;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
