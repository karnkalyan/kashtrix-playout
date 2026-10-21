using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;
using BroadcastPlayout.ViewModels;

namespace BroadcastPlayout.Views;

public partial class ChannelControllerWindow : Window, INotifyPropertyChanged
{
    private readonly MainViewModel _vm;
    private readonly ChannelControllerServer _server = new();
    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _targetIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ChannelDefinition> _definitions = [];
    private ChannelNodeView? _selectedChannel;
    private CgProject? _selectedGraphic;
    private string _listenerStatus = "STARTING";

    public ObservableCollection<ChannelNodeView> Channels { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];
    public ObservableCollection<CgProject> Graphics => _vm.CgProjects;
    public int ConnectedCount => Channels.Count(x => x.IsOnline);
    public int TotalChannelCount => Channels.Count;
    public int SelectedTargetCount => _targetIds.Count;
    public string ListenerStatus { get => _listenerStatus; private set { _listenerStatus = value; Raise(); } }
    public ChannelNodeView? SelectedChannel { get => _selectedChannel; set { _selectedChannel = value; Raise(); } }
    public CgProject? SelectedGraphic { get => _selectedGraphic; set { _selectedGraphic = value; Raise(); } }

    public ChannelControllerWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = this;
        _vm.EnsureCgDemos();
        _vm.ReloadCgProjectsFromDatabase();
        ReloadDefinitions();

        _server.ChannelsChanged += () => Dispatcher.BeginInvoke(RefreshChannels);
        _server.Log += text => Dispatcher.BeginInvoke(() => AddLog(text));
        try
        {
            _server.Start(vm.ControllerPort);
            ListenerStatus = $"LISTENING · TCP {vm.ControllerPort}";
        }
        catch (Exception ex)
        {
            ListenerStatus = "LISTENER ERROR";
            AddLog("Controller listener: " + ex.Message);
        }

        SelectedGraphic = Graphics.FirstOrDefault();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            RefreshChannels();
        };
        _timer.Start();
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        RefreshChannels();
        Closed += (_, _) => { _timer.Stop(); _server.Dispose(); };
    }

    private void ReloadDefinitions()
    {
        _definitions.Clear();
        _definitions.AddRange(ChannelRegistryStore.Load().Where(x => x.Enabled));
    }

    private void RefreshChannels()
    {
        var selectedId = SelectedChannel?.Status.ChannelId;
        var live = _server.Channels.ToDictionary(x => x.ChannelId, StringComparer.OrdinalIgnoreCase);
        var ids = new HashSet<string>(live.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var definition in _definitions) ids.Add(definition.ChannelId);
        ids.Add(_vm.ChannelId);
        _targetIds.RemoveWhere(id => !ids.Contains(id));
        if (_targetIds.Count == 0)
        {
            foreach (var id in live.Keys) _targetIds.Add(id);
            _targetIds.Add(_vm.ChannelId);
        }

        var next = new List<ChannelNodeView>();
        var index = 1;
        foreach (var id in ids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var definition = _definitions.FirstOrDefault(x => x.ChannelId.Equals(id, StringComparison.OrdinalIgnoreCase));
            var isOnline = live.TryGetValue(id, out var liveStatus);
            var status = liveStatus ?? new ChannelStatusSnapshot
            {
                ChannelId = id,
                ChannelName = definition?.ChannelName ?? (id.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase) ? _vm.ChannelName : "Unregistered Channel"),
                Format = definition?.Format ?? (id.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase) ? _vm.PlayoutPreset : "--"),
                State = id.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase) ? "LOCAL" : "OFFLINE",
                ProgramTitle = id.Equals(_vm.ChannelId, StringComparison.OrdinalIgnoreCase) ? "Direct local CG target" : "No client connected",
                NextTitle = definition?.Notes ?? string.Empty
            };

            if (definition is not null)
            {
                if (string.IsNullOrWhiteSpace(status.ChannelName)) status.ChannelName = definition.ChannelName;
                if (string.IsNullOrWhiteSpace(status.Format)) status.Format = definition.Format;
            }

            var node = new ChannelNodeView(index++, status, isOnline, false, OnTargetChanged)
            {
                IsSelected = _targetIds.Contains(id)
            };
            next.Add(node);
        }

        Channels.Clear();
        foreach (var node in next) Channels.Add(node);
        SelectedChannel = Channels.FirstOrDefault(x => x.Status.ChannelId.Equals(selectedId, StringComparison.OrdinalIgnoreCase)) ?? Channels.FirstOrDefault();
        Raise(nameof(ConnectedCount));
        Raise(nameof(TotalChannelCount));
        Raise(nameof(SelectedTargetCount));
    }

    private void OnTargetChanged(ChannelNodeView node, bool selected)
    {
        if (selected) _targetIds.Add(node.Status.ChannelId);
        else _targetIds.Remove(node.Status.ChannelId);
        Raise(nameof(SelectedTargetCount));
    }

    private async void PlayCg_Click(object sender, RoutedEventArgs e) => await RouteAsync("PLAY", "PROGRAM");
    private async void PreviewCg_Click(object sender, RoutedEventArgs e) => await RouteAsync("PLAY", "PREVIEW");
    private async void StopCg_Click(object sender, RoutedEventArgs e) => await RouteAsync("STOP", "PROGRAM");

    private async Task RouteAsync(string action, string bus = "PROGRAM")
    {
        var selectedGraphicId = SelectedGraphic?.Id ?? Guid.Empty;
        _vm.ReloadCgProjectsFromDatabase();
        SelectedGraphic = Graphics.FirstOrDefault(x => x.Id == selectedGraphicId) ?? Graphics.FirstOrDefault();

        var selected = Channels.Where(x => x.IsSelected).ToArray();
        if (selected.Length == 0)
        {
            MessageBox.Show("Select one or more TARGET channels first.", "Kashtrix Channel Controller", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!action.Equals("STOP", StringComparison.OrdinalIgnoreCase) && SelectedGraphic is null)
        {
            MessageBox.Show("Select a graphic asset first.", "Kashtrix Channel Controller", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var allRealTargets = selected.Where(x => !x.IsDemo).Select(x => x.Status.ChannelId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var onlineTargets = selected.Where(x => !x.IsDemo && x.IsOnline).Select(x => x.Status.ChannelId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var demoTargets = selected.Where(x => x.IsDemo).Select(x => x.Status.ChannelId).ToArray();
        var command = new CgRemoteCommand
        {
            CommandId = Guid.NewGuid().ToString("N"),
            Action = action,
            Bus = bus,
            Project = SelectedGraphic
        };

        var localDelivered = 0;
        try { if (allRealTargets.Length > 0) localDelivered = LocalCgCommandBus.Publish(allRealTargets, command); }
        catch (Exception ex) { AddLog("LOCAL CG route warning · " + ex.Message); }

        var networkDelivered = 0;
        if (onlineTargets.Length > 0)
        {
            try { await _server.RouteCgAsync(onlineTargets, command); networkDelivered = onlineTargets.Length; }
            catch (Exception ex) { AddLog("NETWORK CG route warning · " + ex.Message); }
        }

        foreach (var id in demoTargets) AddLog($"DEMO · CG {bus} {action} -> {id}");
        var delivered = localDelivered + networkDelivered + demoTargets.Length;
        if (delivered == 0)
            MessageBox.Show("No selected target accepted the CG command. Start a local Playout or connect a remote client.", "Kashtrix Channel Controller", MessageBoxButton.OK, MessageBoxImage.Information);
        else
            AddLog($"{bus} {action} · {SelectedGraphic?.Name ?? "CG"} · LOCAL {localDelivered} / NETWORK {networkDelivered} / DEMO {demoTargets.Length}");
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var channel in Channels) channel.IsSelected = true;
    }

    private void ClearTargets_Click(object sender, RoutedEventArgs e)
    {
        foreach (var channel in Channels) channel.IsSelected = false;
    }

    private void Manage_Click(object sender, RoutedEventArgs e)
    {
        var window = new ChannelManagerWindow { Owner = this };
        if (window.ShowDialog() == true)
        {
            ReloadDefinitions();
            RefreshChannels();
            AddLog("Channel registry updated.");
        }
    }

    // Demo channel seeding removed - only actual registered and connected channels are displayed.

    private void OpenCg_Click(object sender, RoutedEventArgs e)
    {
        if (!StandaloneAppLauncher.Launch("CGEditor", out var error))
            MessageBox.Show(error, "Kashtrix CG Editor", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ChannelCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is ChannelNodeView node) SelectedChannel = node;
    }

    private void AddLog(string text)
    {
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {text}");
        while (Logs.Count > 150) Logs.RemoveAt(Logs.Count - 1);
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.ToggleMaximize(this);
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowChromeActions.Drag(this, e);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ChannelNodeView : INotifyPropertyChanged
{
    private readonly Action<ChannelNodeView, bool>? _selectionChanged;
    private bool _selected;

    public ChannelNodeView(int index, ChannelStatusSnapshot status, bool isOnline, bool isDemo, Action<ChannelNodeView, bool>? selectionChanged = null)
    {
        Index = index;
        Status = status;
        IsOnline = isOnline;
        IsDemo = isDemo;
        _selectionChanged = selectionChanged;
    }

    public int Index { get; }
    public ChannelStatusSnapshot Status { get; }
    public bool IsOnline { get; }
    public bool IsDemo { get; }
    public Brush StateBrush => IsOnline ? (IsDemo ? Brushes.DeepPink : Brushes.LawnGreen) : Brushes.Gray;
    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            _selectionChanged?.Invoke(this, value);
        }
    }

    public string ElapsedText => TimeSpan.FromSeconds(Math.Max(0, Status.ElapsedSeconds)).ToString(@"hh\:mm\:ss");
    public ImageSource? ProgramImage => Decode(Status.ProgramPreviewJpegBase64);
    public ImageSource? PreviewImage => Decode(Status.PreviewJpegBase64);

    private static ImageSource? Decode(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            var bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit(); image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; image.StreamSource = ms; image.EndInit(); image.Freeze();
            return image;
        }
        catch { return null; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
