using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.IO.MemoryMappedFiles;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Data;
using Kashtrix.OutputEngine.Models;
using Kashtrix.OutputEngine.Services;
using BroadcastPlayout.Models;
using BroadcastPlayout.Outputs;
using ThemedMessageBox = BroadcastPlayout.Views.MessageBox;

namespace Kashtrix.OutputEngine
{
    public class CustomInputItemViewModel : INotifyPropertyChanged
    {
        public PlaylistItem Item { get; set; }

        public string Title => !string.IsNullOrWhiteSpace(Item.Title) ? Item.Title : "Custom Input";
        public string SourceKind => string.IsNullOrWhiteSpace(Item.SourceKind) ? "Custom" : Item.SourceKind;
        public string SourceKindUpper => SourceKind.ToUpperInvariant();
        public string Details => !string.IsNullOrWhiteSpace(Item.VideoDevice)
            ? Item.VideoDevice
            : (!string.IsNullOrWhiteSpace(Item.FilePath) ? Item.FilePath : $"{Item.CaptureWidth}x{Item.CaptureHeight} @ {(Item.SourceFrameRate > 0 ? Item.SourceFrameRate : 25):F0}fps");

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                    OnPropertyChanged(nameof(ActiveVisibility));
                }
            }
        }

        public Visibility ActiveVisibility => IsActive ? Visibility.Visible : Visibility.Collapsed;

        public string BadgeBrush => SourceKind switch
        {
            "DirectShow" => "#059669",
            "NDI" => "#2563EB",
            "RTSP" or "URL" or "Stream" => "#D97706",
            "Screen" or "Desktop" => "#7C3AED",
            _ => "#0D9488"
        };

        public CustomInputItemViewModel(PlaylistItem item, bool isActive = false)
        {
            Item = item;
            _isActive = isActive;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public partial class MainWindow : Window
    {
        private readonly BroadcastOutputEngine _engine = BroadcastOutputEngine.Instance;
        private readonly Scte35DvbService _dvbService = Scte35DvbService.Instance;
        private DispatcherTimer? _uiRefreshTimer;
        private string _activeCategoryFilter = "All";
        private bool _initialized;
        private MemoryMappedFile? _programVideoMap;
        private MemoryMappedViewAccessor? _programVideoView;
        private WriteableBitmap? _programPreviewBitmap;
        private byte[]? _programPreviewPixels;
        private long _programPreviewSequence;
        private DateTime _lastProgramPreviewUtc = DateTime.MinValue;
        private bool _programIsLive;
        private string _activePreviewMapName = string.Empty;
        private OutputInputSource _selectedInputSource = OutputInputSource.PlayoutProgram;
        private string _selectedManualInput = string.Empty;
        private PlaylistItem? _selectedCustomInput;
        private ICollectionView? _outputsView;
        private static readonly Lazy<VirtualOutputBridge> _customPreviewBridge = new(() => new VirtualOutputBridge(VirtualOutputBridge.CustomPreviewVideoMapName, VirtualOutputBridge.CustomPreviewAudioMapName));

        public ObservableCollection<CustomInputItemViewModel> CustomInputViewModels { get; } = new();
        public ObservableCollection<BroadcastEventLog> FilteredLogs { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            BroadcastPlayout.Views.WindowChromeActions.ApplyCleanBorder(this);
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _initialized = true;

            _outputsView = CollectionViewSource.GetDefaultView(_engine.Outputs);
            _outputsView.Filter = item => item is OutputChannel channel && channel.InputSource == _selectedInputSource;
            OutputsDataGrid.ItemsSource = _outputsView;
            LogListBox.ItemsSource = FilteredLogs;

            _dvbService.LogAdded += OnLogReceived;

            _uiRefreshTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                // Follow the default 50 fps Program cadence. A 30 fps sampler creates an
                // uneven 1/2-frame skip pattern for 50 fps crawls and makes a correct ticker
                // look as though it is juddering in the confidence monitor.
                Interval = TimeSpan.FromMilliseconds(20)
            };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();

            TabAll.IsChecked = true;
            RefreshLogView();
            _ = InitCustomInputsAsync();
        }

        private void UiRefreshTimer_Tick(object? sender, EventArgs e)
        {
            var visibleOutputs = _engine.Outputs.Where(c => c.InputSource == _selectedInputSource).ToArray();
            int total = visibleOutputs.Length;
            int running = visibleOutputs.Count(c => c.IsEnabled && c.Status == OutputStatus.Online);
            ActiveOutputsText.Text = $"{running} / {total} ONLINE";

            double aggregateBitrate = visibleOutputs.Where(c => c.IsEnabled && !c.IsCardHardware).Sum(c => c.BitrateMbps);
            AggregateBitrateText.Text = $"{aggregateBitrate:F2} Mbps";

            long totalFrames = visibleOutputs.Sum(c => c.FramesTransmitted);
            TotalFramesText.Text = $"{totalFrames:N0} f";

            long totalDropped = visibleOutputs.Sum(c => c.DroppedFrames);
            double dropPercent = totalFrames > 0 ? ((double)totalDropped / totalFrames) * 100.0 : 0.0;
            DroppedFramesText.Text = $"{totalDropped:N0} f ({dropPercent:F2}%)";
            if (NoOutputsForSourceText != null)
                NoOutputsForSourceText.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;

            DateTime now = DateTime.Now;
            int frameNumber = (int)(now.Millisecond / 20.0);
            MasterTimecodeText.Text = $"{now:HH:mm:ss}:{frameNumber:D2}";
            RefreshProgramPreview();
            var receiving = DateTime.UtcNow - _lastProgramPreviewUtc < TimeSpan.FromSeconds(1);
            var live = receiving && _programIsLive;
            ProgramLiveBadge.Background = live ? new SolidColorBrush(Color.FromRgb(183, 28, 28)) : new SolidColorBrush(Color.FromRgb(55, 65, 81));
            ProgramLiveDot.Fill = live ? new SolidColorBrush(Color.FromRgb(255, 82, 82)) : new SolidColorBrush(Color.FromRgb(156, 163, 175));
            ProgramLiveText.Text = live ? "PGM LIVE" : receiving ? "PGM STANDBY" : "NO PROGRAM SIGNAL";
        }

        private void RefreshProgramPreview()
        {
            if (ProgramPreviewImage == null) return;
            try
            {
                var selectedOutput = OutputsDataGrid?.SelectedItem as OutputChannel;
                var source = selectedOutput?.InputSource ?? _selectedInputSource;
                var manualSource = selectedOutput?.ManualInputSource ?? _selectedManualInput;
                var requestedMap = source switch
                {
                    OutputInputSource.CgProgram => VirtualOutputBridge.CgProgramVideoMapName,
                    OutputInputSource.Manual when string.Equals(manualSource, "CG PROGRAM", StringComparison.OrdinalIgnoreCase) => VirtualOutputBridge.CgProgramVideoMapName,
                    OutputInputSource.Manual when string.Equals(manualSource, "PLAYOUT PROGRAM", StringComparison.OrdinalIgnoreCase) => VirtualOutputBridge.ProgramConfidenceVideoMapName,
                    OutputInputSource.Manual => VirtualOutputBridge.CustomPreviewVideoMapName,
                    _ => VirtualOutputBridge.ProgramConfidenceVideoMapName
                };
                ProgramMonitorLabel.Text = source switch
                {
                    OutputInputSource.CgProgram => "CG PROGRAM BUS · ALPHA/FILL CONFIDENCE",
                    OutputInputSource.Manual => string.IsNullOrWhiteSpace(manualSource) ? "CUSTOM INPUT SOURCE · CONFIDENCE" : $"CUSTOM INPUT · {manualSource}",
                    _ => "PLAYOUT PROGRAM BUS · CONFIDENCE MONITOR"
                };
                if (!string.Equals(requestedMap, _activePreviewMapName, StringComparison.Ordinal))
                {
                    _programVideoView?.Dispose(); _programVideoView = null;
                    _programVideoMap?.Dispose(); _programVideoMap = null;
                    _programPreviewSequence = 0;
                    _lastProgramPreviewUtc = DateTime.MinValue;
                    _activePreviewMapName = requestedMap;
                }
                if (string.IsNullOrWhiteSpace(requestedMap)) return;
                _programVideoMap ??= MemoryMappedFile.OpenExisting(requestedMap, MemoryMappedFileRights.Read);
                _programVideoView ??= _programVideoMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                if (_programVideoView.ReadInt32(0) != 0x4B545856) return;
                var width = _programVideoView.ReadInt32(8);
                var height = _programVideoView.ReadInt32(12);
                var stride = _programVideoView.ReadInt32(16);
                var sequence = _programVideoView.ReadInt64(28);
                var length = _programVideoView.ReadInt32(44);
                var isLive = _programVideoView.ReadInt32(48) != 0;
                if (sequence == _programPreviewSequence || width <= 0 || height <= 0 || stride < width * 4 || length <= 0 || length > stride * height) return;
                var pixels = _programPreviewPixels;
                if (pixels is null || pixels.Length != length)
                    pixels = _programPreviewPixels = new byte[length];
                _programVideoView.ReadArray(256, pixels, 0, pixels.Length);
                // The writer commits by storing sequence after the pixel copy. If it changes
                // while we read, discard this sample and try again on the next render tick.
                if (_programVideoView.ReadInt64(28) != sequence) return;
                if (_programPreviewBitmap is null || _programPreviewBitmap.PixelWidth != width || _programPreviewBitmap.PixelHeight != height)
                {
                    _programPreviewBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                    ProgramPreviewImage.Source = _programPreviewBitmap;
                }
                _programPreviewBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                _programPreviewSequence = sequence;
                _lastProgramPreviewUtc = DateTime.UtcNow;
                _programIsLive = isLive;
            }
            catch
            {
                _programVideoView?.Dispose(); _programVideoView = null;
                _programVideoMap?.Dispose(); _programVideoMap = null;
            }
        }

        private void OnLogReceived(BroadcastEventLog log)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (MatchesFilter(log))
                {
                    FilteredLogs.Insert(0, log);
                    if (FilteredLogs.Count > 500)
                    {
                        FilteredLogs.RemoveAt(FilteredLogs.Count - 1);
                    }
                }
                if (LogCountText != null)
                {
                    LogCountText.Text = $"Events: {_dvbService.Logs.Count}";
                }
            });
        }

        private bool MatchesFilter(BroadcastEventLog log)
        {
            if (_activeCategoryFilter == "All") return true;
            if (_activeCategoryFilter == "OUTPUTS" && (log.Category.Contains("OUTPUT", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("ALERT", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("ENGINE", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("HARDWARE", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("ROUTING", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("CONFIG", StringComparison.OrdinalIgnoreCase) || log.Level.Equals("ERROR", StringComparison.OrdinalIgnoreCase) || log.Level.Equals("WARN", StringComparison.OrdinalIgnoreCase))) return true;
            if (_activeCategoryFilter == "DVB" && log.Category.Contains("DVB", StringComparison.OrdinalIgnoreCase)) return true;
            if (_activeCategoryFilter == "SCTE-35" && log.Category.Contains("SCTE-35", StringComparison.OrdinalIgnoreCase)) return true;
            if (_activeCategoryFilter == "MOS" && log.Category.Contains("MOS", StringComparison.OrdinalIgnoreCase)) return true;
            if (_activeCategoryFilter == "HLS/MPD" && (log.Category.Contains("HLS", StringComparison.OrdinalIgnoreCase) || log.Category.Contains("MPD", StringComparison.OrdinalIgnoreCase))) return true;
            return false;
        }

        private void RefreshLogView()
        {
            if (!_initialized || _dvbService == null || FilteredLogs == null) return;
            FilteredLogs.Clear();
            foreach (var log in _dvbService.Logs.Where(MatchesFilter).Take(500))
            {
                FilteredLogs.Add(log);
            }
            if (LogCountText != null)
            {
                LogCountText.Text = $"Events: {_dvbService.Logs.Count}";
            }
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (!_initialized || _dvbService == null || FilteredLogs == null) return;
            if (sender is not RadioButton rb) return;
            string name = rb.Name;

            _activeCategoryFilter = name switch
            {
                "TabOutputs" => "OUTPUTS",
                "TabDvb" => "DVB",
                "TabScte35" => "SCTE-35",
                "TabMos" => "MOS",
                "TabHlsMpd" => "HLS/MPD",
                _ => "All"
            };

            RefreshLogView();
        }

        private void InputSourceTab_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initialized) return;
            if (sender is not RadioButton rb) return;

            switch (rb.Name)
            {
                case "SourceTabPlayout":
                    _selectedInputSource = OutputInputSource.PlayoutProgram;
                    _selectedManualInput = string.Empty;
                    if (CustomInputsDrawer != null) CustomInputsDrawer.Visibility = Visibility.Collapsed;
                    if (ActiveInputSourceLabel != null) ActiveInputSourceLabel.Text = "PLAYOUT PROGRAM BUS";
                    _dvbService.Log("ROUTING", "Output Engine source routed to Playout Program Bus", "INFO");
                    break;

                case "SourceTabCg":
                    _selectedInputSource = OutputInputSource.CgProgram;
                    _selectedManualInput = string.Empty;
                    if (CustomInputsDrawer != null) CustomInputsDrawer.Visibility = Visibility.Collapsed;
                    if (ActiveInputSourceLabel != null) ActiveInputSourceLabel.Text = "CG CONTROLLER PROGRAM BUS";
                    _dvbService.Log("ROUTING", "Output Engine source routed to CG Controller Program Bus (Alpha/Fill+Key)", "INFO");
                    break;

                case "SourceTabCustom":
                    _selectedInputSource = OutputInputSource.Manual;
                    if (CustomInputsDrawer != null) CustomInputsDrawer.Visibility = Visibility.Visible;
                    if (_selectedCustomInput != null)
                    {
                        CustomInputHub.Instance.EnsureRunning(_selectedCustomInput);
                        if (ActiveInputSourceLabel != null) ActiveInputSourceLabel.Text = $"CUSTOM ({_selectedCustomInput.SourceKind}): {_selectedManualInput}";
                        _dvbService.Log("ROUTING", $"Output Engine source routed to Custom Input: {_selectedManualInput}", "INFO");
                    }
                    else if (CustomInputViewModels.Count > 0)
                    {
                        RouteAndSetOutput(CustomInputViewModels[0]);
                    }
                    else
                    {
                        ConfigureCustomInput();
                    }
                    break;
            }
            RefreshOutputSourceView();
        }

        private void RefreshOutputSourceView()
        {
            _outputsView?.Refresh();
            if (OutputsDataGrid != null)
                OutputsDataGrid.SelectedItem = _outputsView?.Cast<object>().FirstOrDefault();
            _activePreviewMapName = string.Empty;
        }

        private void ConfigureCustomInput_Click(object sender, RoutedEventArgs e)
        {
            ConfigureCustomInput();
        }

        private void ConfigureCustomInput()
        {
            try
            {
                var dlg = new BroadcastPlayout.Views.InputSourceWindow
                {
                    Owner = this
                };
                if (dlg.ShowDialog() == true && dlg.ResultItem != null)
                {
                    var item = dlg.ResultItem;
                    var vm = new CustomInputItemViewModel(item, isActive: true);
                    foreach (var other in CustomInputViewModels) other.IsActive = false;
                    CustomInputViewModels.Insert(0, vm);
                    RouteAndSetOutput(vm);
                }
                else if (string.IsNullOrWhiteSpace(_selectedManualInput))
                {
                    if (SourceTabPlayout != null) SourceTabPlayout.IsChecked = true;
                }
            }
            catch (Exception ex)
            {
                ThemedMessageBox.Show(this, $"Failed to open custom input selector: {ex.Message}", "Input Source Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task InitCustomInputsAsync()
        {
            try
            {
                var capture = await BroadcastPlayout.Services.InputDeviceService.EnumerateDirectShowAsync();
                foreach (var dev in capture.VideoDevices)
                {
                    var item = new PlaylistItem
                    {
                        Title = dev,
                        SourceKind = "DirectShow",
                        FilePath = $"video={dev}",
                        VideoDevice = dev,
                        AudioDevice = capture.AudioDevices.FirstOrDefault() ?? string.Empty,
                        InputFormat = "dshow",
                        InputOptions = "-rtbufsize 512M",
                        IsLiveSource = true,
                        CaptureWidth = 1920,
                        CaptureHeight = 1080,
                        SourceFrameRate = 0
                    };
                    CustomInputViewModels.Add(new CustomInputItemViewModel(item));
                }

                CustomInputViewModels.Add(new CustomInputItemViewModel(new PlaylistItem
                {
                    Title = "Desktop Screen Capture (Primary Display)",
                    SourceKind = "Screen",
                    FilePath = "desktop",
                    InputFormat = "gdigrab",
                    InputOptions = "-framerate 30 -draw_mouse 1",
                    IsLiveSource = true,
                    CaptureWidth = 1920,
                    CaptureHeight = 1080,
                    SourceFrameRate = 30
                }));

                CustomInputViewModels.Add(new CustomInputItemViewModel(new PlaylistItem
                {
                    Title = "RTSP Master Ingest Feed",
                    SourceKind = "RTSP",
                    FilePath = "rtsp://127.0.0.1:8554/live",
                    InputFormat = "rtsp",
                    InputOptions = "-rtsp_transport tcp -buffer_size 1024000",
                    IsLiveSource = true,
                    CaptureWidth = 1920,
                    CaptureHeight = 1080,
                    SourceFrameRate = 25
                }));

                var first = CustomInputViewModels.FirstOrDefault();
                if (first != null)
                {
                    first.IsActive = true;
                    _selectedCustomInput = first.Item;
                    _selectedManualInput = !string.IsNullOrWhiteSpace(first.Item.FilePath) ? first.Item.FilePath : first.Item.Title;
                }
            }
            catch { }
        }

        private async void RescanCustomInputs_Click(object sender, RoutedEventArgs e)
        {
            CustomInputViewModels.Clear();
            await InitCustomInputsAsync();
            _dvbService.Log("INPUT", $"Rescanned capture devices. Found {CustomInputViewModels.Count} custom source(s).", "INFO");
        }

        private void CustomInputCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2 && (sender as FrameworkElement)?.DataContext is CustomInputItemViewModel vm)
            {
                RouteAndSetOutput(vm);
                e.Handled = true;
            }
        }

        private void RouteCustomItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CustomInputItemViewModel vm)
            {
                RouteAndSetOutput(vm);
            }
        }

        private void AddOutputForCustomItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is CustomInputItemViewModel vm)
            {
                AddOutputForCustom(vm);
            }
        }

        private void AddCustomOutput_Click(object sender, RoutedEventArgs e)
        {
            var activeVm = CustomInputViewModels.FirstOrDefault(x => x.IsActive) ?? CustomInputViewModels.FirstOrDefault();
            AddOutputForCustom(activeVm);
        }

        private void RouteAndSetOutput(CustomInputItemViewModel vm)
        {
            foreach (var other in CustomInputViewModels) other.IsActive = false;
            vm.IsActive = true;

            _selectedCustomInput = vm.Item;
            _selectedInputSource = OutputInputSource.Manual;
            _selectedManualInput = !string.IsNullOrWhiteSpace(vm.Item.FilePath) ? vm.Item.FilePath : vm.Item.Title;

            if (SourceTabCustom != null) SourceTabCustom.IsChecked = true;
            if (CustomInputsDrawer != null) CustomInputsDrawer.Visibility = Visibility.Visible;
            if (ActiveInputSourceLabel != null)
            {
                ActiveInputSourceLabel.Text = $"CUSTOM ({vm.SourceKind}): {_selectedManualInput}";
            }

            CustomInputHub.Instance.SetInput(vm.Item);
            _dvbService.Log("ROUTING", $"Routed custom input '{vm.Title}' [{vm.SourceKind}] to Output Engine", "SUCCESS");

            RefreshOutputSourceView();

            var existingManualOutputs = _engine.Outputs.Where(x => x.InputSource == OutputInputSource.Manual).ToList();
            if (existingManualOutputs.Count == 0)
            {
                AddOutputForCustom(vm);
            }
            else
            {
                foreach (var ch in existingManualOutputs)
                {
                    ch.ManualInputSource = _selectedManualInput;
                    ch.ManualInputKind = vm.SourceKind;
                    ch.ManualInputFormat = vm.Item.InputFormat ?? string.Empty;
                    ch.ManualInputOptions = vm.Item.InputOptions ?? string.Empty;
                    ch.ManualVideoDevice = vm.Item.VideoDevice ?? string.Empty;
                    ch.ManualAudioDevice = vm.Item.AudioDevice ?? string.Empty;
                    ch.ManualAlternateAudioUrl = vm.Item.AlternateAudioUrl ?? string.Empty;
                    ch.ManualIsLiveSource = vm.Item.IsLiveSource;
                    ch.ManualCaptureWidth = vm.Item.CaptureWidth;
                    ch.ManualCaptureHeight = vm.Item.CaptureHeight;
                    ch.ManualSourceFrameRate = vm.Item.SourceFrameRate;
                }
                _engine.SaveOutputs();
                _outputsView?.Refresh();
            }
        }

        private void AddOutputForCustom(CustomInputItemViewModel? vm)
        {
            if (vm != null)
            {
                foreach (var other in CustomInputViewModels) other.IsActive = false;
                vm.IsActive = true;
                _selectedCustomInput = vm.Item;
                _selectedInputSource = OutputInputSource.Manual;
                _selectedManualInput = !string.IsNullOrWhiteSpace(vm.Item.FilePath) ? vm.Item.FilePath : vm.Item.Title;
                if (SourceTabCustom != null) SourceTabCustom.IsChecked = true;
                if (CustomInputsDrawer != null) CustomInputsDrawer.Visibility = Visibility.Visible;
                if (ActiveInputSourceLabel != null) ActiveInputSourceLabel.Text = $"CUSTOM ({vm.SourceKind}): {_selectedManualInput}";
                CustomInputHub.Instance.SetInput(vm.Item);
            }

            var dialog = new AddOutputDialog(OutputInputSource.Manual, _selectedManualInput, _selectedCustomInput) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultChannel != null)
            {
                _engine.Outputs.Add(dialog.ResultChannel);
                _engine.SaveOutputs();
                _outputsView?.Refresh();
                OutputsDataGrid.SelectedItem = dialog.ResultChannel;
                _engine.StartOutput(dialog.ResultChannel);
                _dvbService.Log("CONFIG", $"Provisioned output '{dialog.ResultChannel.Name}' [{dialog.ResultChannel.ProtocolDisplayName}] for custom input '{_selectedCustomInput?.Title}'", "SUCCESS", dialog.ResultChannel.Name);
            }
        }

        private void OutputsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row?.Item is OutputChannel channel)
            {
                EditChannel(channel);
                e.Handled = true;
            }
        }

        private void EditChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                EditChannel(channel);
            }
        }

        private void EditChannel(OutputChannel channel)
        {
            bool wasRunning = channel.IsEnabled || channel.Status == OutputStatus.Online;
            var dialog = new AddOutputDialog(channel) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultChannel != null)
            {
                _engine.SaveOutputs();
                _outputsView?.Refresh();
                _dvbService.Log("CONFIG", $"Modified settings for output '{channel.Name}' [{channel.ProtocolDisplayName}] ({channel.RasterFormat} @ {channel.TargetFps} fps)", "SUCCESS", channel.Name);
                if (wasRunning)
                {
                    _dvbService.Log("OUTPUT", $"Applying updated settings to running output '{channel.Name}'...", "INFO", channel.Name);
                    _engine.RestartOutput(channel);
                }
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent) return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void EnsureStandbyCustomPreview()
        {
            if (_selectedInputSource == OutputInputSource.Manual && _selectedCustomInput != null)
            {
                CustomInputHub.Instance.EnsureRunning(_selectedCustomInput);
            }
        }

        private void StopStandbyCustomPreview()
        {
            // CustomInputHub manages shared lifetime across all outputs and monitor
        }

        private void AddOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddOutputDialog(_selectedInputSource, _selectedManualInput, _selectedCustomInput) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultChannel != null)
            {
                _engine.Outputs.Add(dialog.ResultChannel);
                _engine.SaveOutputs();
                _outputsView?.Refresh();
                OutputsDataGrid.SelectedItem = dialog.ResultChannel;
                _engine.StartOutput(dialog.ResultChannel);
                _dvbService.Log("CONFIG", $"Provisioned new output destination: {dialog.ResultChannel.Name} [{dialog.ResultChannel.ProtocolDisplayName}] ({dialog.ResultChannel.GpuEncoder}) routed from {dialog.ResultChannel.InputSourceDisplayName}", "SUCCESS", dialog.ResultChannel.Name);
            }
        }

        private void InspectAlert_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                var dialog = new InspectOutputDialog(channel) { Owner = this };
                dialog.ShowDialog();
            }
        }

        private void ToggleChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                _engine.ToggleOutput(channel);
            }
        }

        private void StartChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                _engine.StartOutput(channel);
            }
        }

        private void StopChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                _engine.StopOutput(channel);
            }
        }

        private void RestartChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel)
            {
                _engine.RestartOutput(channel);
            }
        }

        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OutputChannel channel) return;
            if (ThemedMessageBox.Show(this, $"Delete output '{channel.Name}'?", "Kashtrix Output Engine", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _engine.DeleteOutput(channel);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => BroadcastPlayout.Views.WindowChromeActions.Drag(this, e);
        private void Minimize_Click(object sender, RoutedEventArgs e) => BroadcastPlayout.Views.WindowChromeActions.Minimize(this);
        private void Maximize_Click(object sender, RoutedEventArgs e) => BroadcastPlayout.Views.WindowChromeActions.ToggleMaximize(this);
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            CustomInputHub.Instance.Stop();
            Close();
        }

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ch in _engine.Outputs.Where(x => x.InputSource == _selectedInputSource))
                _engine.StartOutput(ch);
            _dvbService.Log("ENGINE", $"All {_selectedInputSource} outputs started by operator", "INFO");
        }

        private void StopAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ch in _engine.Outputs.Where(x => x.InputSource == _selectedInputSource))
                _engine.StopOutput(ch);
            _dvbService.Log("ENGINE", $"All {_selectedInputSource} outputs stopped by operator", "WARN");
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            _dvbService.Logs.Clear();
            FilteredLogs.Clear();
            if (LogCountText != null)
            {
                LogCountText.Text = "Events: 0";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _initialized = false;
            try { _uiRefreshTimer?.Stop(); } catch { }
            try { _programVideoView?.Dispose(); } catch { }
            _programVideoView = null;
            try { _programVideoMap?.Dispose(); } catch { }
            _programVideoMap = null;
            try { CustomInputHub.Instance.Stop(); } catch { }
            try { if (_dvbService != null) _dvbService.LogAdded -= OnLogReceived; } catch { }
            try { _engine.Dispose(); } catch { }
            base.OnClosed(e);
        }
    }
}
