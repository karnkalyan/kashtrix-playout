using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;
using BroadcastPlayout.ViewModels;
using Forms = System.Windows.Forms;
using OpenFileDialog = BroadcastPlayout.Views.KashtrixFileDialog;
using SaveFileDialog = BroadcastPlayout.Views.KashtrixSaveFileDialog;

namespace BroadcastPlayout.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;
    public SettingsWindow(MainViewModel vm)
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);
        _vm = vm;
        DataContext = vm;
        InitConditionalCgTab();
    }

    private readonly System.Collections.ObjectModel.ObservableCollection<ConditionalCgRule> _channelRules = [];
    private string _activeChannelId = string.Empty;

    private void InitConditionalCgTab()
    {
        ConditionalRulesGrid.ItemsSource = _channelRules;
        var channels = ChannelRegistryStore.Load();
        if (!channels.Any(x => x.ChannelId.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase)))
        {
            channels.Insert(0, new ChannelDefinition
            {
                ChannelId = _vm.ChannelId,
                ChannelName = _vm.ChannelName,
                Format = _vm.PlayoutPreset,
                Enabled = true,
                ConditionalCgRules = _vm.ConditionalCgRules.ToList().ToList()
            });
        }
        ConditionalChannelCombo.ItemsSource = channels;
        ConditionalChannelCombo.DisplayMemberPath = "ChannelName";
        ConditionalChannelCombo.SelectedValuePath = "ChannelId";

        var selected = channels.FirstOrDefault(x => x.ChannelId.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase)) ?? channels.FirstOrDefault();
        if (selected is not null)
        {
            ConditionalChannelCombo.SelectedItem = selected;
        }
    }

    private void ConditionalChannelCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ConditionalChannelCombo.SelectedItem is not ChannelDefinition ch) return;
        _activeChannelId = ch.ChannelId;
        ConditionalChannelSummary.Text = $"Rules apply exclusively to playout channel: {ch.ChannelName} ({ch.ChannelId})";

        _channelRules.Clear();
        if (ch.ChannelId.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var r in _vm.ConditionalCgRules) _channelRules.Add(r);
        }
        else
        {
            var stored = ch.ConditionalCgRules ?? [];
            foreach (var r in stored) _channelRules.Add(r);
        }
    }

    private void AddConditionalRule_Click(object sender, RoutedEventArgs e)
    {
        var targetProj = _vm.CgProjects.FirstOrDefault()?.Name ?? "Market · Live Stock & Indices Ticker";
        var rule = new ConditionalCgRule
        {
            Name = $"New Rule {_channelRules.Count + 1}",
            ChannelId = _activeChannelId,
            ConditionExpression = "category = music and title = arijit",
            TargetCgProjectName = targetProj,
            DelaySeconds = 1.0,
            DurationSeconds = 12.0,
            Enabled = true
        };
        _channelRules.Add(rule);
        ConditionalRulesGrid.SelectedItem = rule;
    }

    private void DeleteConditionalRule_Click(object sender, RoutedEventArgs e)
    {
        if (ConditionalRulesGrid.SelectedItem is ConditionalCgRule sel)
        {
            _channelRules.Remove(sel);
        }
    }

    private void SaveConditionalRules_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_activeChannelId)) return;

            var channels = ChannelRegistryStore.Load();
            var ch = channels.FirstOrDefault(x => x.ChannelId.Equals(_activeChannelId, StringComparison.OrdinalIgnoreCase));
            if (ch is null)
            {
                ch = new ChannelDefinition { ChannelId = _activeChannelId, ChannelName = _activeChannelId, Format = _vm.PlayoutPreset, Enabled = true };
                channels.Add(ch);
            }
            ch.ConditionalCgRules = _channelRules.ToList();
            ChannelRegistryStore.Save(channels);

            if (_activeChannelId.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase))
            {
                _vm.ConditionalCgRules.Clear();
                foreach (var r in _channelRules) _vm.ConditionalCgRules.Add(r);
                _vm.SaveConditionalCgRules();
            }

            MessageBox.Show($"Conditional CG rules saved successfully for channel {_activeChannelId} ({_channelRules.Count} rules active).", "Kashtrix Conditional CG", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Conditional CG Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; return; }
        base.OnPreviewKeyDown(e);
    }
    private void NavList_SelectionChanged(object sender,System.Windows.Controls.SelectionChangedEventArgs e){if(SettingsTabs is not null&&NavList.SelectedIndex>=0&&NavList.SelectedIndex<SettingsTabs.Items.Count)SettingsTabs.SelectedIndex=NavList.SelectedIndex;}
    private void Save_Click(object sender,RoutedEventArgs e){try{_vm.SaveSettings();_vm.SaveSchedules();MessageBox.Show("Settings saved for all Kashtrix applications.","Kashtrix Settings");}catch(Exception ex){MessageBox.Show(ex.Message,"Settings",MessageBoxButton.OK,MessageBoxImage.Error);}}
    private void LoadDemo_Click(object sender,RoutedEventArgs e){_vm.LoadDemoWorkspace();ChannelRegistryStore.Save(DemoDataFactory.CreateChannels());MessageBox.Show("Demo workspace loaded.","Kashtrix");}
    private void Close_Click(object sender,RoutedEventArgs e)=>Close();
    private void OpenInputs_Click(object sender,RoutedEventArgs e){var w=new InputSourceWindow{Owner=this};w.ShowDialog();}
    private void OpenScheduler_Click(object sender,RoutedEventArgs e)=>Launch("Scheduler");
    private void OpenCg_Click(object sender,RoutedEventArgs e)=>Launch("CGEditor");
    private void OpenCgController_Click(object sender,RoutedEventArgs e)=>Launch("CGController");
    private void OpenChannelController_Click(object sender,RoutedEventArgs e)=>Launch("ChannelController");
    private void OpenHaController_Click(object sender,RoutedEventArgs e)=>Launch("HAController");
    private void OpenApiGateway_Click(object sender,RoutedEventArgs e)=>Launch("ApiGateway");
    private void OpenNrcs_Click(object sender,RoutedEventArgs e)=>Launch("NRCS");
    private void OpenPrompter_Click(object sender,RoutedEventArgs e)=>Launch("Prompter");
    private void OpenQc_Click(object sender,RoutedEventArgs e)=>Launch("QCController");
    private void OpenFileManager_Click(object sender,RoutedEventArgs e)=>Launch("FileManager");
    private void OpenMam_Click(object sender,RoutedEventArgs e)=>Launch("MAM");
    private void OpenIngest_Click(object sender,RoutedEventArgs e)=>Launch("IngestServer");
    private void OpenVideoProcessor_Click(object sender,RoutedEventArgs e){var w=new VideoProcessorWindow(_vm){Owner=this};w.Show();}
    private void Launch(string key){if(!StandaloneAppLauncher.Launch(key,out var error))MessageBox.Show(error,"Kashtrix",MessageBoxButton.OK,MessageBoxImage.Information);}
    private void RunWizard_Click(object sender,RoutedEventArgs e){var s=SettingsStore.Load();s.FirstRunCompleted=false;SettingsStore.Save(s);MessageBox.Show("The setup wizard will run next time Playout starts.","Kashtrix Setup");}

    private void OpenEpgSettings_Click(object sender,RoutedEventArgs e) => new EpgOutputSettingsWindow(_vm){Owner=this}.ShowDialog();
    private void OpenAuditLog_Click(object sender,RoutedEventArgs e) => new AuditLogWindow{Owner=this}.Show();
    private void ExportAsRun_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            var path = new AsRunLogService().ExportCsv(_vm.ChannelId);
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute=true });
            MessageBox.Show($"As-run reconciliation CSV exported:\n{path}","Kashtrix As-Run",MessageBoxButton.OK,MessageBoxImage.Information);
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"Kashtrix As-Run",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
    private void OpenUserManagement_Click(object sender,RoutedEventArgs e)
    {
        if (!AppSession.IsAdmin) { MessageBox.Show("Administrator role required.","Kashtrix Security",MessageBoxButton.OK,MessageBoxImage.Warning); return; }
        new UserManagementWindow{Owner=this}.ShowDialog();
    }
    private void SaveEpg_Click(object sender, RoutedEventArgs e)
    {
        _vm.Settings.Epg.Normalize();
        _vm.SaveSettings();
        AuditLogService.Write("EPG_SETTINGS", _vm.ChannelName, "EPG / auto-graphics settings saved from Settings.");
        MessageBox.Show("EPG / Auto Graphics settings saved.", "Kashtrix EPG");
    }
    private void RebuildCgDemos_Click(object sender, RoutedEventArgs e)
    {
        _vm.RebuildCgDemoPack();
        MessageBox.Show("The canonical 200-project CG demo pack has been rebuilt and saved.", "Kashtrix CG");
    }
    private void AddCategory_Click(object sender, RoutedEventArgs e) => _vm.AddCategory();
    private void ResetCategories_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Reset categories to broadcast defaults?", "Kashtrix Categories", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            _vm.ResetCategoriesToDefault();
    }

    private void BrowseFiller_Click(object sender,RoutedEventArgs e){using var d=new Forms.FolderBrowserDialog{Description="Select filler media folder",UseDescriptionForTitle=true};if(d.ShowDialog()==Forms.DialogResult.OK)_vm.FillerFolder=d.SelectedPath;}
    private void BrowseSctePlaylist_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Filter = "Kashtrix Playlist (*.ktxplay;*.json;*.m3u)|*.ktxplay;*.json;*.m3u|All Files (*.*)|*.*" };
        if (d.ShowDialog() == true)
        {
            _vm.Settings.ProfessionalBroadcast.ScteOnBreakPlaylistFile = d.FileName;
        }
    }

    private void ImportPreset_Click(object sender,RoutedEventArgs e)
    {
        var d=new OpenFileDialog{Filter="Kashtrix channel preset|*.ktxchannel.json;*.json|JSON|*.json"};if(d.ShowDialog()!=true)return;
        try{var preset=ChannelPresetStore.Import(d.FileName);_vm.ApplyChannelPreset(preset);MessageBox.Show($"Preset applied: {preset.ChannelName}","Channel Preset");}catch(Exception ex){MessageBox.Show(ex.Message,"Channel Preset",MessageBoxButton.OK,MessageBoxImage.Error);}
    }
    private void ExportPreset_Click(object sender,RoutedEventArgs e)
    {
        var safe=string.Concat(_vm.ChannelName.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
        var d=new SaveFileDialog{Filter="Kashtrix channel preset|*.ktxchannel.json",FileName=safe+".ktxchannel.json"};if(d.ShowDialog()!=true)return;
        try{_vm.SaveSettings();ChannelPresetStore.Export(d.FileName,SettingsStore.Load());MessageBox.Show("Channel preset exported.","Channel Preset");}catch(Exception ex){MessageBox.Show(ex.Message,"Channel Preset",MessageBoxButton.OK,MessageBoxImage.Error);}
    }

    private void LaunchOutputEngine_Click(object sender, RoutedEventArgs e) => Launch("OutputEngine");

    private void AddOutputRoute_Click(object sender, RoutedEventArgs e)
    {
        _vm.AddOutputRoute();
    }

    private void RemoveOutputRoute_Click(object sender, RoutedEventArgs e)
    {
        var route = _vm.AdditionalOutputs.LastOrDefault();
        if (route is not null) _vm.RemoveOutputRoute(route);
    }

    private void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Clear Chromium CG cache and media thumbnails the next time Kashtrix starts?", "Kashtrix Recovery", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        RecoveryService.ScheduleCacheClear();
        MessageBox.Show("Cache cleanup scheduled. Close and reopen Kashtrix Playout to complete it.", "Kashtrix Recovery");
    }

    private void FactoryReset_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("This will delete the local Kashtrix settings, rundown recovery database, categories and caches on next start, then run the setup wizard. Continue?", "Factory Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        RecoveryService.ScheduleFactoryReset();
        MessageBox.Show("Factory reset scheduled. Close Kashtrix and start it again.", "Factory Reset");
    }

    private void RefreshDeckLinkDevices_Click(object sender, RoutedEventArgs e)
    {
        _vm.RefreshDeckLinkDevices();
        MessageBox.Show($"DeckLink scan complete.\nSelected: {_vm.DeckLinkDeviceText}", "Kashtrix DeckLink", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void RefreshOutputDiagnostics_Click(object sender, RoutedEventArgs e) => _vm.RefreshOutputDiagnostics();
    private void DebugOutputs_Click(object sender, RoutedEventArgs e) => RunTool("Debug-Outputs.ps1");
    private void BuildVirtualOutput_Click(object sender,RoutedEventArgs e)=>RunTool("Build-VirtualOutput.ps1");
    private void RegisterVirtualOutput_Click(object sender,RoutedEventArgs e)=>RunTool("Register-VirtualOutput.ps1");
    private void RunTool(string name)
    {
        try
        {
            var script = ToolLocator.FindTool(name);
            if (string.IsNullOrWhiteSpace(script))
            {
                MessageBox.Show($"Tool not found: {name}. Run tools\\Verify-And-Build.ps1 once from the extracted source folder.", "Virtual Output", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var root = Path.GetDirectoryName(Path.GetDirectoryName(script)) ?? AppContext.BaseDirectory;
            Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\"") { WorkingDirectory=root, UseShellExecute=true });
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"Virtual Output",MessageBoxButton.OK,MessageBoxImage.Warning);}
    }
    private void TitleBar_MouseLeftButtonDown(object sender,MouseButtonEventArgs e)=>WindowChromeActions.Drag(this,e);
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.Minimize(this);
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.ToggleMaximize(this);

}
