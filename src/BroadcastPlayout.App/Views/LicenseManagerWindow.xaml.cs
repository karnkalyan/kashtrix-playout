using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BroadcastPlayout.Services;

namespace BroadcastPlayout.Views;

public sealed class ModuleGateItem : INotifyPropertyChanged
{
    private bool _isActive;
    public string AppKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Capabilities { get; init; } = string.Empty;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusBadgeText));
                OnPropertyChanged(nameof(StatusBadgeBg));
                OnPropertyChanged(nameof(StatusBadgeFg));
                OnPropertyChanged(nameof(StatusBorderBrush));
            }
        }
    }

    private bool _canActivate;
    public bool CanActivate
    {
        get => _canActivate;
        set
        {
            if (_canActivate != value)
            {
                _canActivate = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusBadgeText => IsActive ? "ACTIVE · LICENSED" : (CanActivate ? "INACTIVE (LICENSED)" : "UNLICENSED");
    public Brush StatusBadgeBg => IsActive ? new SolidColorBrush(Color.FromRgb(16, 42, 24)) : new SolidColorBrush(Color.FromRgb(30, 41, 59));
    public Brush StatusBadgeFg => IsActive ? new SolidColorBrush(Color.FromRgb(85, 200, 120)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
    public Brush StatusBorderBrush => IsActive ? new SolidColorBrush(Color.FromRgb(36, 92, 54)) : new SolidColorBrush(Color.FromRgb(51, 65, 85));

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public partial class LicenseManagerWindow : Window
{
    private readonly ObservableCollection<ModuleGateItem> _modules = [];

    public LicenseManagerWindow()
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);
        ModuleGatesControl.ItemsSource = _modules;
        LoadLicenseDetails();

        KashtrixLicenseAuthority.OnLicenseDelivered += HandleLicenseDelivered;
        KashtrixLicenseAuthority.OnServerConnectionChanged += HandleServerConnectionChanged;
        KashtrixLicenseAuthority.OnLicenseRevokedRemotely += HandleLicenseRevokedRemotely;

        Closed += (_, _) =>
        {
            KashtrixLicenseAuthority.OnLicenseDelivered -= HandleLicenseDelivered;
            KashtrixLicenseAuthority.OnServerConnectionChanged -= HandleServerConnectionChanged;
            KashtrixLicenseAuthority.OnLicenseRevokedRemotely -= HandleLicenseRevokedRemotely;
        };
        KashtrixLicenseAuthority.StartAutoResolveListener();
    }

    private void HandleServerConnectionChanged(bool connected)
    {
        Dispatcher.InvokeAsync(() => UpdateServerConnectionUI(connected));
    }

    private void UpdateServerConnectionUI(bool connected)
    {
        if (connected)
        {
            ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(85, 200, 120));
            ServerStatusText.Text = "Connected";
            ServerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(133, 222, 128));
        }
        else
        {
            ServerStatusDot.Fill = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            var p = KashtrixLicenseAuthority.Current;
            if (p.IsActivated)
            {
                ServerStatusText.Text = "Disconnected (Offline Mode Active)";
                ServerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
            else
            {
                ServerStatusText.Text = "Disconnected";
                ServerStatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }
        }
    }

    private void HandleLicenseRevokedRemotely(string ev, string reason)
    {
        Dispatcher.InvokeAsync(() =>
        {
            LoadLicenseDetails();
            StatusMessageText.Text = $"License event received: {reason}";
            MessageBox.Show($"License notice: {reason}\n\nBroadcast applications authorization updated.", "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    private void HandleLicenseDelivered(bool success, string msg)
    {
        if (!success) return;
        Dispatcher.InvokeAsync(() =>
        {
            LoadLicenseDetails();
            StatusMessageText.Text = "✓ Remote auto-resolve: " + msg;
        });
    }

    private void LoadLicenseDetails()
    {
        try
        {
            var p = KashtrixLicenseAuthority.Current;
            LicensedToBox.Text = string.IsNullOrWhiteSpace(p.CustomerName) ? "Chief Transmission Engineer" : p.CustomerName;
            OrgBox.Text = string.IsNullOrWhiteSpace(p.LicensedCompany) ? "Kashtrix Broadcast Systems Ltd." : p.LicensedCompany;
            LicenseKeyBox.Text = p.SerialNumber;
            LicenseIdBox.Text = string.IsNullOrWhiteSpace(p.LicenseId) ? "UNREGISTERED-TRIAL" : p.LicenseId;
            NodeIdBox.Text = KashtrixLicenseAuthority.HardwareId;
            KtxTokenBox.Text = KashtrixLicenseAuthority.ProvisioningKtxCode;

            ActivationStatusText.Text = string.IsNullOrWhiteSpace(p.ActivationStatus) ? "Unlicensed · Activation Required" : p.ActivationStatus;
            ExpirationText.Text = string.IsNullOrWhiteSpace(p.ExpirationDate) ? "Requires License Key Activation" : p.ExpirationDate;
            MaxResText.Text = $"{p.PlayoutMaxResolution} UHD (SD/HD/FHD/4K/8K)";
            SupportContractText.Text = string.IsNullOrWhiteSpace(p.SupportContract) ? "Evaluation Support · Unlicensed" : p.SupportContract;
            UpdateServerConnectionUI(KashtrixLicenseAuthority.IsServerConnected);

            if (p.IsActivated)
            {
                TierBannerText.Text = "BROADCAST ENTERPRISE MASTER · LICENSED";
                TierBannerSubtext.Text = "Centralized Omni License Protocol v2 · Authoritative per-module execution gating · 16 Channels & 8K UHD transmission";
                BannerIconText.Text = "🛡";
                BannerIconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                BannerIconBorder.Background = new SolidColorBrush(Color.FromRgb(29, 42, 58));
                TopBannerBorder.Background = new SolidColorBrush(Color.FromRgb(16, 24, 36));
                TopBannerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 61, 84));

                BannerBadgeText.Text = "ACTIVE & AUTHORITATIVE";
                BannerBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(133, 222, 128));
                BannerBadgeDot.Fill = new SolidColorBrush(Color.FromRgb(85, 200, 120));
                BannerBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(22, 48, 32));
                BannerBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 200, 120));
                ActivationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(133, 222, 128));
            }
            else
            {
                TierBannerText.Text = "KASHTRIX SUITE · UNLICENSED EVALUATION";
                TierBannerSubtext.Text = "Centralized Omni License Protocol v2 · Activation key required to unlock broadcast transmission engines.";
                BannerIconText.Text = "🔒";
                BannerIconBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                BannerIconBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                TopBannerBorder.Background = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                TopBannerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));

                BannerBadgeText.Text = "UNLICENSED · ACTIVATION REQUIRED";
                BannerBadgeText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                BannerBadgeDot.Fill = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                BannerBadgeBorder.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                BannerBadgeBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85));
                ActivationStatusText.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            }

            PopulateModuleGates(p);

            var activePlayouts = p.AuthorizedNodes.Count(n => n.IsAuthorized && n.ApplicationType.Contains("Playout", StringComparison.OrdinalIgnoreCase));
            PlayoutQuotaSummaryText.Text = $"Playout Quota: {activePlayouts} / {p.PlayoutMaxChannels} Channels Authorized · Max Res: {p.PlayoutMaxResolution}";
            AuthorizedNodesControl.ItemsSource = null;
            AuthorizedNodesControl.ItemsSource = p.AuthorizedNodes.ToList();
        }
        catch { }
    }

    private void PopulateModuleGates(LicensePolicy p)
    {
        _modules.Clear();

        var definitions = new List<(string Key, string Name, string Desc, string Caps, bool Active)>
        {
            ("Playout", "Kashtrix Playout Master Engine", "Multi-format broadcast transmission automation (SD, HD, FHD, 4K, 8K hierarchical)", "Features: EPG · SCTE-35/104 · MOS · 16 Channels · 7.1 Audio", p.PlayoutActive),
            ("OutputEngine", "Kashtrix Output Engine Master", "Hardware SDI output, IP network streaming & virtual broadcast output hub", "Interfaces: Matrox · DeckLink · AJA · NDI · RTMP · UDP · SRT · Virtual Out", p.OutputEngineActive),
            ("InputEngine", "Kashtrix Input Engine", "Live baseband video ingestion, frame synchronizer & remote camera hub", "Feeds: SDI DeckLink · DirectShow · NDI · RTMP/UDP Streams · YouTube", p.InputEngineActive),
            ("HAController", "Kashtrix High-Availability Redundancy", "1+1 hot mirror redundancy controller with automatic heartbeat failover", "Modes: Mirror Sync · Automated IP Failover · Active Tally Telemetry", p.HaControllerActive),
            ("NRCS", "Kashtrix NRCS Newsroom Computer System", "Newsroom rundown management, story authoring & MOS teleprompter sync", "Tools: Dual Calendar (AD / Bikram Sambat BS) · Story Rundown · MOS Protocol", p.NrcsActive),
            ("ApiGateway", "Kashtrix API Gateway & Web MCR", "REST / WebSocket automation hub and remote web browser control room", "Protocols: HTTP REST · WebSocket Telemetry · TCP Remote Control", p.ApiGatewayActive),
            ("CGController", "Kashtrix CG Controller", "On-air graphic playout, external DeckLink/NDI outputs, tickers & DVE layers", "Capabilities: External SDI/NDI Output · Tickers · Clocks · Layer Stacking", p.CgControllerActive),
            ("CGEditor", "Kashtrix CG Designer", "Multi-layer vector graphic template designer with 8K/4K resolution presets", "Features: 8K/4K Presets · Quick Edit · Light Sweep FX · 3D Box Projection", p.CgEditorActive),
            ("ChannelController", "Kashtrix Channel Controller", "Multi-channel supervision, emergency bypass switcher & CG play control", "Supervision: Multi-Channel Monitoring · Emergency Cutover · CG Take Bus", p.ChannelControllerActive),
            ("IngestServer", "Kashtrix Ingest Server", "Automated multi-channel scheduled recording for baseband and live streams", "Recording: Scheduled Ingest · SDI Capture · NDI Stream Ingest · 16-Channel Quota", p.IngestActive),
            ("MAM", "Kashtrix Media Asset Management", "Enterprise broadcast media database, metadata indexing & fast proxy player", "Features: Media Indexing · Fast Scrubbing · Proxy Generation · Tagging", p.MamActive),
            ("Multiview", "Kashtrix Multiviewer Monitor", "Multi-input confidence monitoring wall with tally borders and live audio VU meters", "Monitoring: 16 Input Feeds · Realtime Audio VU · Red/Green Tally", p.MultiviewerActive),
            ("PlaylistEditor", "Kashtrix Playlist Editor", "Broadcast playlist sequencer, sub-clip event trimming and metadata editor", "Tools: Multi-Channel Rundowns · Secondary Events · Direct Push to Playout", p.PlaylistEditorActive),
            ("Scheduler", "Kashtrix Broadcast Scheduler", "24/7 automated daypart scheduling, program clocks and traffic reconciliation", "Automation: 24/7 Grid · Daypart Clocks · Fixed Time Events · EPG Export", p.PlaylistSchedulerActive),
            ("Prompter", "Kashtrix Studio Teleprompter", "Studio newsroom teleprompter with talent confidence screen and NDI feed", "Output: Multi-Display · NDI Talent Stream · Speed Scroll · Story Cues", p.PrompterActive),
            ("QCController", "Kashtrix QC Controller", "Technical broadcast validation, freeze frame, black frame & silence detection", "Validation: Audio Silence · Video Freeze · Black Detection · Format Compliance", p.QcControllerActive),
            ("FileManager", "Kashtrix File Manager", "Broadcast media preparation, format validation & workstation staging tool", "Staging: Storage Health · Media Move/Copy · Video Header Validation", p.FileManagerActive)
        };

        foreach (var def in definitions)
        {
            var isAuthorized = p.IsActivated && p.IsModuleAuthorizedByToken(def.Key);
            _modules.Add(new ModuleGateItem
            {
                AppKey = def.Key,
                Name = def.Name,
                Description = def.Desc,
                Capabilities = def.Caps,
                IsActive = isAuthorized && def.Active,
                CanActivate = isAuthorized
            });
        }
    }

    private void AutoGetLicense_Click(object sender, RoutedEventArgs e)
    {
        StatusMessageText.Text = "Connecting to license server to wait for automatic license dispatch...";
        KashtrixLicenseAuthority.TriggerServerReconnect();
        MessageBox.Show(
            "Automatic License Delivery listener connected to central server.\n\n" +
            "The client socket is now online and waiting for server dispatch. When the administrator issues or approves the license from the server dashboard, this suite will activate automatically without manual copying.",
            "Auto Get License from Server",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ActivateLicense_Click(object sender, RoutedEventArgs e)
    {
        var key = LicenseKeyBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show("Please enter a valid Kashtrix license serial key to activate.", "Activate License", MessageBoxButton.OK, MessageBoxImage.Warning);
            LicenseKeyBox.Focus();
            return;
        }

        // Pass null operator/company so details are read directly from the authenticated token claims
        if (KashtrixLicenseAuthority.ActivateLicense(key, null, null, out var message))
        {
            KashtrixLicenseAuthority.TriggerServerReconnect();
            LoadLicenseDetails();
            StatusMessageText.Text = message;
            MessageBox.Show(message, "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(message, "License Activation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ResetToUnlicensed_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "Are you sure you want to reset this workstation to UNLICENSED evaluation state?\n\nAll broadcast modules and transmission engines will be locked until activated with a valid serial key.",
            "Reset License to Unlicensed",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            KashtrixLicenseAuthority.ResetToUnlicensed();
            LoadLicenseDetails();
            StatusMessageText.Text = "Kashtrix Broadcast Suite reset to UNLICENSED evaluation mode.";
            MessageBox.Show("Software reset to UNLICENSED state.\n\nAll modular application gates are now locked.", "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ActivateAll_Click(object sender, RoutedEventArgs e)
    {
        var p = KashtrixLicenseAuthority.Current;
        if (!p.IsActivated)
        {
            MessageBox.Show("Kashtrix Broadcast Suite is UNLICENSED.\n\nPlease activate with a valid broadcast license key first.", "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int activatedCount = 0;
        foreach (var m in _modules)
        {
            if (m.CanActivate)
            {
                m.IsActive = true;
                activatedCount++;
            }
        }
        StatusMessageText.Text = $"{activatedCount} suite module(s) marked active. Click 'SAVE LICENSE PROFILE' to apply.";
    }

    private void DeactivateAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var m in _modules) m.IsActive = false;
        StatusMessageText.Text = "All module gates deactivated. Click 'SAVE LICENSE PROFILE' to enforce offline gates.";
    }

    private void SavePolicy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p = KashtrixLicenseAuthority.Current;
            if (!p.IsActivated)
            {
                MessageBox.Show("Kashtrix Broadcast Suite is UNLICENSED.\n\nPlease activate with a valid broadcast license key before updating module authorization.", "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            p.CustomerName = LicensedToBox.Text.Trim();
            p.LicensedCompany = OrgBox.Text.Trim();
            p.RegisteredOrg = OrgBox.Text.Trim();
            p.SerialNumber = LicenseKeyBox.Text.Trim();

            // Apply module states directly to policy ONLY if certified by token
            foreach (var m in _modules)
            {
                var active = m.CanActivate && m.IsActive;
                switch (m.AppKey)
                {
                    case "Playout": p.PlayoutActive = active; break;
                    case "OutputEngine": p.OutputEngineActive = active; break;
                    case "InputEngine": p.InputEngineActive = active; break;
                    case "HAController": p.HaControllerActive = active; break;
                    case "NRCS": p.NrcsActive = active; break;
                    case "ApiGateway": p.ApiGatewayActive = active; break;
                    case "CGController": p.CgControllerActive = active; break;
                    case "CGEditor": p.CgEditorActive = active; break;
                    case "ChannelController": p.ChannelControllerActive = active; break;
                    case "IngestServer": p.IngestActive = active; break;
                    case "MAM": p.MamActive = active; break;
                    case "Multiview": p.MultiviewerActive = active; break;
                    case "PlaylistEditor": p.PlaylistEditorActive = active; break;
                    case "Scheduler": p.PlaylistSchedulerActive = active; break;
                    case "Prompter": p.PrompterActive = active; break;
                    case "QCController": p.QcControllerActive = active; break;
                    case "FileManager": p.FileManagerActive = active; break;
                }
            }

            KashtrixLicenseAuthority.SavePolicy(p);
            LoadLicenseDetails();
            StatusMessageText.Text = $"License policy profile successfully verified and saved ({DateTime.Now:HH:mm:ss}).";
            MessageBox.Show("License policy profile successfully updated.", "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save policy: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyKtx_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var ktx = KtxTokenBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(ktx))
            {
                Clipboard.SetText(ktx);
                StatusMessageText.Text = "KTX Device Provisioning Token copied to clipboard.";
                MessageBox.Show("KTX Device Provisioning Token copied to clipboard!\n\nYou can register or assign this token in the central Kashtrix License Server.", "KTX Token Copied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not copy KTX code: {ex.Message}", "Clipboard Notice", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportCertificate_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new KashtrixFileDialog
        {
            Owner = this,
            Title = "Export License Certificate",
            IsSaveMode = true,
            FileName = "Kashtrix-Broadcast-License.json",
            DefaultExt = ".json"
        };
        dialog.SetFilter("JSON Files|*.json|All Files|*.*");
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var p = KashtrixLicenseAuthority.Current;
                var content = JsonSerializer.Serialize(new
                {
                    LicensedTo = string.IsNullOrWhiteSpace(p.CustomerName) ? LicensedToBox.Text : p.CustomerName,
                    Company = string.IsNullOrWhiteSpace(p.LicensedCompany) ? OrgBox.Text : p.LicensedCompany,
                    NodeId = NodeIdBox.Text,
                    LicenseId = p.IsActivated ? p.LicenseId : "UNLICENSED",
                    Tier = p.IsActivated ? p.TierName : "Kashtrix Broadcast Suite (Evaluation)",
                    Status = p.IsActivated ? "Active · Licensed" : "Unlicensed · Activation Required",
                    ExpirationDate = string.IsNullOrWhiteSpace(p.ExpirationDate) ? "Perpetual / Activation Required" : p.ExpirationDate,
                    ExportedAt = DateTime.UtcNow,
                    ActiveModules = _modules.Where(m => m.IsActive).Select(m => m.AppKey).ToArray(),
                    DeactivatedModules = _modules.Where(m => !m.IsActive).Select(m => m.AppKey).ToArray()
                }, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(dialog.FileName, content);
                MessageBox.Show($"License certificate exported to:\n{dialog.FileName}", "License Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not export certificate: {ex.Message}", "License Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLicenseDetails();
        StatusMessageText.Text = $"Details refreshed at {DateTime.Now:HH:mm:ss}.";
    }

    private void AddWorkstationNode_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AuthorizeWorkstationDialog
        {
            Owner = this
        };
        if (dlg.ShowDialog() == true)
        {
            LoadLicenseDetails();
            StatusMessageText.Text = $"Workstation node '{dlg.CreatedNode?.ChannelName}' authorized successfully.";
        }
    }

    private void RevokeNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.Tag is string nodeId)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to revoke authorization for this workstation/channel node?",
                "Revoke Authorization",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes)
            {
                if (KashtrixLicenseAuthority.RevokeWorkstationNode(nodeId, out var msg))
                {
                    LoadLicenseDetails();
                    StatusMessageText.Text = msg;
                    MessageBox.Show(msg, "Kashtrix License Authority", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(msg, "Revocation Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowChromeActions.Drag(this, e);
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.Minimize(this);
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.ToggleMaximize(this);
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
