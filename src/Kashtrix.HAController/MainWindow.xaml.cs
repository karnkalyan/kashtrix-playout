using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BroadcastPlayout.Professional;
using BroadcastPlayout.Services;

namespace Kashtrix.HAController;

public partial class MainWindow : Window
{
    private readonly HaCoordinatorService _coordinator = new();
    private readonly System.Windows.Threading.DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly BroadcastPlayout.Models.AppSettings _appSettings;
    private FileSystemWatcher? _controlWatcher;

    private readonly Dictionary<string, HaHeartbeat> _customNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HaHeartbeat> _demoNodes = new(StringComparer.OrdinalIgnoreCase);
    private bool _demoClusterActive = false;
    private double _localPlayoutPosition = 142.5;

    public MainWindow()
    {
        InitializeComponent();
        _appSettings = SettingsStore.Load();
        Start();
        StartControlWatcher();

        // Load demo cluster initially so user immediately sees redundancy topology and active demos!
        LoadDemoCluster();

        _timer.Tick += (_, _) => OnTimerTick();
        _timer.Start();
    }

    private void Start()
    {
        try
        {
            _coordinator.Start(_appSettings.ProfessionalBroadcast);
            StatusText.Text = $"Coordinator listening {_appSettings.ProfessionalBroadcast.HaMulticastAddress}:{_appSettings.ProfessionalBroadcast.HaHeartbeatPort} · control {_appSettings.ProfessionalBroadcast.HaControlPort}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "HA start failed: " + ex.Message;
        }
    }

    private void OnTimerTick()
    {
        // 1. Detect and synthesize active local playout if running
        DetectLocalPlayout();

        // 2. Pulse custom nodes
        foreach (var key in _customNodes.Keys.ToList())
        {
            var n = _customNodes[key];
            var updated = n with { PositionSeconds = n.PositionSeconds + 0.5, UtcTicks = DateTime.UtcNow.Ticks };
            _customNodes[key] = updated;
            _coordinator.IngestHeartbeat(updated);
        }

        // 3. Pulse demo nodes
        if (_demoClusterActive)
        {
            foreach (var key in _demoNodes.Keys.ToList())
            {
                var n = _demoNodes[key];
                if (n.Role == "FAILED") continue; // failed node does not heartbeat
                var updated = n with { PositionSeconds = n.PositionSeconds + 0.5, UtcTicks = DateTime.UtcNow.Ticks };
                _demoNodes[key] = updated;
                _coordinator.IngestHeartbeat(updated);
            }
        }

        Refresh();
    }

    private void DetectLocalPlayout()
    {
        try
        {
            var playoutProcs = Process.GetProcessesByName("BroadcastPlayout.App");
            var localRunning = playoutProcs.Length > 0;
            var localNodeId = Environment.MachineName + "-PLAYOUT";
            var localChannel = string.IsNullOrWhiteSpace(_appSettings.ChannelId) ? "CH-01" : _appSettings.ChannelId;

            // If local playout process is running or local configured node is desired, ensure heartbeat is present
            if (localRunning)
            {
                _localPlayoutPosition += 0.5;
                var localBeat = new HaHeartbeat(
                    NodeId: localNodeId,
                    ChannelId: localChannel,
                    Priority: 100,
                    Capacity: 1,
                    ConfiguredMode: "PRIMARY",
                    Role: "ACTIVE",
                    OutputArmed: true,
                    CurrentItemId: "Local-Active-Playout.mp4",
                    PositionSeconds: _localPlayoutPosition,
                    UtcTicks: DateTime.UtcNow.Ticks
                );
                _coordinator.IngestHeartbeat(localBeat);
            }
        }
        catch { }
    }

    private void LoadDemoCluster()
    {
        _demoClusterActive = true;
        _demoNodes.Clear();

        // CH-01 1+1 Pair
        _demoNodes["CH-01|TX01-PRIMARY"] = new HaHeartbeat("TX01-PRIMARY", "CH-01", 100, 1, "PRIMARY", "ACTIVE", true, "NEWS-HD-PACKAGE-01.mp4", 320.0, DateTime.UtcNow.Ticks);
        _demoNodes["CH-01|TX01-HOTSTANDBY"] = new HaHeartbeat("TX01-HOTSTANDBY", "CH-01", 90, 1, "BACKUP", "STANDBY", false, "NEWS-HD-PACKAGE-01.mp4", 320.0, DateTime.UtcNow.Ticks);

        // CH-02 1+1 Pair
        _demoNodes["CH-02|TX02-PRIMARY"] = new HaHeartbeat("TX02-PRIMARY", "CH-02", 100, 1, "PRIMARY", "ACTIVE", true, "LIVE-SPORTS-MATCH.ts", 845.5, DateTime.UtcNow.Ticks);
        _demoNodes["CH-02|TX02-HOTSTANDBY"] = new HaHeartbeat("TX02-HOTSTANDBY", "CH-02", 90, 1, "BACKUP", "STANDBY", false, "LIVE-SPORTS-MATCH.ts", 845.5, DateTime.UtcNow.Ticks);

        // Shared N+M Pool Node
        _demoNodes["N+M-SHARED|POOL-SRV-DR01"] = new HaHeartbeat("POOL-SRV-DR01", "N+M-SHARED", 60, 4, "POOL", "STANDBY", false, "WARM-HOT-STANDBY", 0.0, DateTime.UtcNow.Ticks);

        foreach (var beat in _demoNodes.Values)
        {
            _coordinator.IngestHeartbeat(beat);
        }

        DemoClusterButton.Content = "CLEAR DEMO CLUSTER";
        ClusterModeBadge.Text = "DEMO CLUSTER ACTIVE";
    }

    private void ClearDemoCluster()
    {
        _demoClusterActive = false;
        foreach (var key in _demoNodes.Keys)
        {
            var parts = key.Split('|');
            if (parts.Length == 2) _coordinator.RemoveNode(parts[0], parts[1]);
        }
        _demoNodes.Clear();

        DemoClusterButton.Content = "LOAD DEMO CLUSTER";
        ClusterModeBadge.Text = "BROADCAST CLUSTER";
        Refresh();
    }

    private void ToggleDemoCluster_Click(object sender, RoutedEventArgs e)
    {
        if (_demoClusterActive) ClearDemoCluster();
        else LoadDemoCluster();
    }

    private void AddPlayout_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddHaPlayoutWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.CreatedHeartbeat is not null)
        {
            var beat = dlg.CreatedHeartbeat;
            var key = $"{beat.ChannelId}|{beat.NodeId}";
            _customNodes[key] = beat;
            _coordinator.IngestHeartbeat(beat);
            Refresh();
            StatusText.Text = $"Added playout node {beat.NodeId} for channel {beat.ChannelId} ({beat.ConfiguredMode}).";
        }
    }

    private void Refresh()
    {
        var selected = (NodeGrid.SelectedItem as HaNodeSnapshot)?.NodeId;
        var channel = (NodeGrid.SelectedItem as HaNodeSnapshot)?.ChannelId;
        var rows = _coordinator.Snapshot();
        HaControlBus.PublishSnapshot(rows);
        NodeGrid.ItemsSource = rows;
        if (selected is not null)
        {
            NodeGrid.SelectedItem = rows.FirstOrDefault(x => x.NodeId == selected && x.ChannelId == channel);
        }
        StatusText.Text = $"{rows.Count} active heartbeat(s) · {rows.Select(x => x.ChannelId).Distinct(StringComparer.OrdinalIgnoreCase).Count()} channel(s) · coordinator {(CoordinatorCheck.IsChecked == true ? "ON" : "MONITOR ONLY")}";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void CoordinatorCheck_Click(object sender, RoutedEventArgs e)
    {
        if (CoordinatorCheck.IsChecked == true)
        {
            _coordinator.Stop();
            Start();
        }
        else _coordinator.Stop();
    }

    private void ForceActive_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGrid.SelectedItem is not HaNodeSnapshot n) return;
        EnsureCoordinator();
        _coordinator.ForceActive(n.ChannelId, n.NodeId, TimeSpan.FromMinutes(10), "MANUAL FORCE ACTIVE");

        // If in demo cluster, promote node
        var key = $"{n.ChannelId}|{n.NodeId}";
        if (_demoNodes.TryGetValue(key, out var beat))
        {
            _demoNodes[key] = beat with { Role = "ACTIVE", OutputArmed = true };
            _coordinator.IngestHeartbeat(_demoNodes[key]);

            // Demote any other nodes on same channel
            foreach (var otherKey in _demoNodes.Keys.Where(k => k.StartsWith(n.ChannelId + "|") && k != key).ToList())
            {
                _demoNodes[otherKey] = _demoNodes[otherKey] with { Role = "STANDBY", OutputArmed = false };
                _coordinator.IngestHeartbeat(_demoNodes[otherKey]);
            }
        }

        StatusText.Text = $"Forced {n.ChannelId} active on {n.NodeId} for 10 minutes.";
        Refresh();
    }

    private void SimulateFailover_Click(object sender, RoutedEventArgs e)
    {
        var snapshot = _coordinator.Snapshot();
        var target = NodeGrid.SelectedItem as HaNodeSnapshot ?? snapshot.FirstOrDefault(x => x.RuntimeRole == "ACTIVE");
        if (target is null)
        {
            MessageBox.Show("Please select an active node to simulate failover.", "Simulate Failover", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var key = $"{target.ChannelId}|{target.NodeId}";

        // Mark this node failed
        if (_demoNodes.TryGetValue(key, out var beat))
        {
            _demoNodes[key] = beat with { Role = "FAILED", OutputArmed = false };
            _coordinator.IngestHeartbeat(_demoNodes[key]);
        }
        else
        {
            _coordinator.RemoveNode(target.ChannelId, target.NodeId);
        }

        // Find candidate standby to failover to
        var standby = snapshot.FirstOrDefault(x => x.ChannelId == target.ChannelId && x.NodeId != target.NodeId);
        if (standby is not null)
        {
            var standbyKey = $"{standby.ChannelId}|{standby.NodeId}";
            if (_demoNodes.TryGetValue(standbyKey, out var standbyBeat))
            {
                _demoNodes[standbyKey] = standbyBeat with { Role = "ACTIVE", OutputArmed = true };
                _coordinator.IngestHeartbeat(_demoNodes[standbyKey]);
            }
            EnsureCoordinator();
            _coordinator.ForceActive(standby.ChannelId, standby.NodeId, TimeSpan.FromMinutes(10), "AUTOMATIC FAILOVER (SIMULATED)");
            StatusText.Text = $"⚡ FAILOVER TRIGGERED: {target.NodeId} FAILED! Standby {standby.NodeId} took over {target.ChannelId} (ARMED).";
        }
        else
        {
            StatusText.Text = $"⚡ FAILOVER TRIGGERED: {target.NodeId} FAILED. No standby available on channel {target.ChannelId}.";
        }

        Refresh();
    }

    private void RestoreNode_Click(object sender, RoutedEventArgs e)
    {
        var snapshot = _coordinator.Snapshot();
        var selected = NodeGrid.SelectedItem as HaNodeSnapshot;

        // Restore any failed demo nodes or selected node
        foreach (var key in _demoNodes.Keys.ToList())
        {
            var beat = _demoNodes[key];
            if (beat.Role == "FAILED" || (selected is not null && selected.NodeId == beat.NodeId))
            {
                var role = beat.ConfiguredMode == "PRIMARY" ? "ACTIVE" : "STANDBY";
                var armed = beat.ConfiguredMode == "PRIMARY";
                _demoNodes[key] = beat with { Role = role, OutputArmed = armed };
                _coordinator.IngestHeartbeat(_demoNodes[key]);
            }
        }

        if (selected is not null)
        {
            _coordinator.Release(selected.ChannelId);
        }

        StatusText.Text = "Restored node state and released channel to nominal configuration.";
        Refresh();
    }

    private void Release_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGrid.SelectedItem is not HaNodeSnapshot n) return;
        EnsureCoordinator();
        _coordinator.Release(n.ChannelId);
        StatusText.Text = $"{n.ChannelId} released to automatic N+M election.";
    }

    private async void Inhibit_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGrid.SelectedItem is not HaNodeSnapshot n) return;
        var key = $"{n.ChannelId}|{n.NodeId}";
        if (_demoNodes.TryGetValue(key, out var beat))
        {
            _demoNodes[key] = beat with { OutputArmed = false };
            _coordinator.IngestHeartbeat(_demoNodes[key]);
            Refresh();
            StatusText.Text = $"Output for {n.NodeId} inhibited.";
            return;
        }
        var r = await PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { ChannelId = n.ChannelId, Action = "output_arm", Parameters = new() { { "armed", "false" } } }, TimeSpan.FromSeconds(2));
        StatusText.Text = r.Message;
    }

    private async void Arm_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGrid.SelectedItem is not HaNodeSnapshot n) return;
        var key = $"{n.ChannelId}|{n.NodeId}";
        if (_demoNodes.TryGetValue(key, out var beat))
        {
            _demoNodes[key] = beat with { OutputArmed = true };
            _coordinator.IngestHeartbeat(_demoNodes[key]);
            Refresh();
            StatusText.Text = $"Output for {n.NodeId} armed.";
            return;
        }
        var r = await PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { ChannelId = n.ChannelId, Action = "output_arm", Parameters = new() { { "armed", "true" } } }, TimeSpan.FromSeconds(2));
        StatusText.Text = r.Message;
    }

    private void RemoveNode_Click(object sender, RoutedEventArgs e)
    {
        if (NodeGrid.SelectedItem is not HaNodeSnapshot n) return;
        var key = $"{n.ChannelId}|{n.NodeId}";
        _customNodes.Remove(key);
        _demoNodes.Remove(key);
        _coordinator.RemoveNode(n.ChannelId, n.NodeId);
        Refresh();
        StatusText.Text = $"Node {n.NodeId} removed from HA cluster.";
    }

    private void EnsureCoordinator()
    {
        if (CoordinatorCheck.IsChecked != true)
        {
            CoordinatorCheck.IsChecked = true;
            Start();
        }
    }

    private void StartControlWatcher()
    {
        Directory.CreateDirectory(HaControlBus.Inbox);
        _controlWatcher?.Dispose();
        _controlWatcher = new FileSystemWatcher(HaControlBus.Inbox, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _controlWatcher.Created += (_, e) => QueueControl(e.FullPath);
        _controlWatcher.Renamed += (_, e) => QueueControl(e.FullPath);
        foreach (var file in Directory.EnumerateFiles(HaControlBus.Inbox, "*.json")) QueueControl(file);
    }

    private void QueueControl(string path)
    {
        _ = Task.Run(async () =>
        {
            for (var i = 0; i < 8; i++)
            {
                if (!File.Exists(path)) return;
                if (HaControlBus.TryConsume(path, out var command) && command is not null)
                {
                    Dispatcher.Invoke(() => ExecuteControl(command));
                    return;
                }
                await Task.Delay(35);
            }
        });
    }

    private void ExecuteControl(HaControlCommand command)
    {
        try
        {
            EnsureCoordinator();
            var action = (command.Action ?? string.Empty).Trim().ToLowerInvariant();
            if (action == "force_active")
            {
                if (string.IsNullOrWhiteSpace(command.ChannelId) || string.IsNullOrWhiteSpace(command.NodeId))
                    throw new InvalidOperationException("channelId and nodeId are required.");
                _coordinator.ForceActive(command.ChannelId, command.NodeId, TimeSpan.FromSeconds(Math.Clamp(command.DurationSeconds, 5, 86400)), command.Reason);
                HaControlBus.Complete(command, true, "OK", $"{command.ChannelId} forced active on {command.NodeId}.");
            }
            else if (action == "release")
            {
                if (string.IsNullOrWhiteSpace(command.ChannelId))
                    throw new InvalidOperationException("channelId is required.");
                _coordinator.Release(command.ChannelId);
                HaControlBus.Complete(command, true, "OK", $"{command.ChannelId} released to automatic election.");
            }
            else HaControlBus.Complete(command, false, "UNSUPPORTED_ACTION", "Unsupported HA action: " + command.Action);
        }
        catch (Exception ex)
        {
            HaControlBus.Complete(command, false, "COMMAND_FAILED", ex.GetBaseException().Message);
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _timer.Stop();
        try { _controlWatcher?.Dispose(); } catch { }
        _coordinator.Dispose();
    }
}
