using System;
using System.Windows;
using System.Windows.Input;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public partial class LicenseActivationPromptWindow : Window
{
    public LicenseActivationPromptWindow(string appName, string reason)
    {
        InitializeComponent();
        AppTitleText.Text = $"{appName} · Activation Required";
        ReasonMessageText.Text = string.IsNullOrWhiteSpace(reason)
            ? $"Kashtrix Broadcast Suite is UNLICENSED. '{appName}' cannot run without an active broadcast license."
            : reason;

        KtxTokenBox.Text = KashtrixLicenseAuthority.ProvisioningKtxCode;
        HwidBox.Text = KashtrixLicenseAuthority.HardwareId;
    }

    public static void ShowPrompt(string appName, string reason)
    {
        try
        {
            var prompt = new LicenseActivationPromptWindow(appName, reason);
            prompt.ShowDialog();
        }
        catch
        {
            try
            {
                var ktx = KashtrixLicenseAuthority.ProvisioningKtxCode;
                MessageBox.Show(
                    $"[Kashtrix License Authority]\n\n{reason}\n\nKTX Activation Token:\n{ktx}\n\nPlease open Kashtrix License Manager to activate.",
                    appName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch { }
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void CopyKtx_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var token = KtxTokenBox.Text.Trim();
            Clipboard.SetText(token);
            CopyKtxButton.Content = "✓ COPIED!";
            FooterFeedbackText.Text = "KTX provisioning token copied to clipboard! Provide this to your administrator.";
            FooterFeedbackText.Foreground = System.Windows.Media.Brushes.LightGreen;
        }
        catch
        {
            FooterFeedbackText.Text = "Failed to copy to clipboard. Please select and copy manually.";
        }
    }

    private void CopyHwid_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(HwidBox.Text.Trim());
            FooterFeedbackText.Text = "Hardware ID (HWID) copied to clipboard!";
            FooterFeedbackText.Foreground = System.Windows.Media.Brushes.LightSkyBlue;
        }
        catch { }
    }

    private void OpenLicenseManager_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mgr = new LicenseManagerWindow();
            mgr.ShowDialog();

            // Check if activation succeeded while dialog was open
            if (KashtrixLicenseAuthority.IsActivated)
            {
                FooterFeedbackText.Text = "Suite is now ACTIVATED! You may restart the application.";
                FooterFeedbackText.Foreground = System.Windows.Media.Brushes.LightGreen;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open License Manager: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
