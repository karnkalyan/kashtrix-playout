using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO.MemoryMappedFiles;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Kashtrix.OutputTypes.Models;
using Kashtrix.OutputTypes.Services;

namespace Kashtrix.OutputTypes
{
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
        private long _programPreviewSequence;

        public ObservableCollection<BroadcastEventLog> FilteredLogs { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _initialized = true;

            OutputsDataGrid.ItemsSource = _engine.Outputs;
            LogListBox.ItemsSource = FilteredLogs;

            _dvbService.LogAdded += OnLogReceived;

            _uiRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();

            TabAll.IsChecked = true;
            RefreshLogView();
        }

        private void UiRefreshTimer_Tick(object? sender, EventArgs e)
        {
            int total = _engine.Outputs.Count;
            int running = _engine.Outputs.Count(c => c.IsEnabled && c.Status == OutputStatus.Online);
            ActiveOutputsText.Text = $"{running} / {total} ONLINE";

            double aggregateBitrate = _engine.Outputs.Where(c => c.IsEnabled).Sum(c => c.BitrateMbps);
            AggregateBitrateText.Text = $"{aggregateBitrate:F2} Mbps";

            long totalFrames = _engine.Outputs.Sum(c => c.FramesTransmitted);
            TotalFramesText.Text = $"{totalFrames:N0} f";

            long totalDropped = _engine.Outputs.Sum(c => c.DroppedFrames);
            double dropPercent = totalFrames > 0 ? ((double)totalDropped / totalFrames) * 100.0 : 0.0;
            DroppedFramesText.Text = $"{totalDropped:N0} f ({dropPercent:F2}%)";

            DateTime now = DateTime.Now;
            int frameNumber = (int)(now.Millisecond / 20.0);
            MasterTimecodeText.Text = $"{now:HH:mm:ss}:{frameNumber:D2}";
            RefreshProgramPreview();
        }

        private void RefreshProgramPreview()
        {
            if (ProgramPreviewImage == null) return;
            try
            {
                _programVideoMap ??= MemoryMappedFile.OpenExisting("KashtrixPlayout.VirtualOutput.Video.v1", MemoryMappedFileRights.Read);
                _programVideoView ??= _programVideoMap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                if (_programVideoView.ReadInt32(0) != 0x4B545856) return;
                var width = _programVideoView.ReadInt32(8);
                var height = _programVideoView.ReadInt32(12);
                var stride = _programVideoView.ReadInt32(16);
                var sequence = _programVideoView.ReadInt64(28);
                var length = _programVideoView.ReadInt32(44);
                if (sequence == _programPreviewSequence || width <= 0 || height <= 0 || stride < width * 4 || length <= 0 || length > stride * height) return;
                var pixels = new byte[length];
                _programVideoView.ReadArray(256, pixels, 0, pixels.Length);
                if (_programPreviewBitmap is null || _programPreviewBitmap.PixelWidth != width || _programPreviewBitmap.PixelHeight != height)
                {
                    _programPreviewBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                    ProgramPreviewImage.Source = _programPreviewBitmap;
                }
                _programPreviewBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
                _programPreviewSequence = sequence;
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
                "TabDvb" => "DVB",
                "TabScte35" => "SCTE-35",
                "TabMos" => "MOS",
                "TabHlsMpd" => "HLS/MPD",
                _ => "All"
            };

            RefreshLogView();
        }

        private void AddOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddOutputDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultChannel != null)
            {
                _engine.Outputs.Add(dialog.ResultChannel);
                _dvbService.Log("CONFIG", $"Provisioned new output destination: {dialog.ResultChannel.Name} [{dialog.ResultChannel.ProtocolDisplayName}] ({dialog.ResultChannel.GpuEncoder})", "SUCCESS");
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
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel) _engine.StartOutput(channel);
        }

        private void StopChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel) _engine.StopOutput(channel);
        }

        private void RestartChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OutputChannel channel) _engine.RestartOutput(channel);
        }

        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not OutputChannel channel) return;
            if (MessageBox.Show($"Delete output '{channel.Name}'?", "Output Engine", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                _engine.DeleteOutput(channel);
        }

        private void TriggerScte35_Click(object sender, RoutedEventArgs e)
        {
            var splice = _dvbService.TriggerScte35Splice(30.0, "splice_insert");
            MessageBox.Show($"Injected SCTE-35 Digital Program Insertion Cue:\n\nEvent ID: {splice.EventId}\nCommand: {splice.CommandType}\nDuration: {splice.DurationSeconds}s\nPTS Hex: {splice.PtsTimestampHex}\nState: {splice.State}\nProgram Splice Flag: 1\nOut-Of-Network: TRUE",
                            "SCTE-35 Splice Trigger Inserted",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void StartAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ch in _engine.Outputs)
            {
                ch.IsEnabled = true;
                ch.Status = OutputStatus.Online;
            }
            _dvbService.Log("ENGINE", "All broadcast outputs resumed by operator", "INFO");
        }

        private void StopAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var ch in _engine.Outputs)
            {
                ch.IsEnabled = false;
                ch.Status = OutputStatus.Standby;
            }
            _dvbService.Log("ENGINE", "All broadcast outputs halted by operator", "WARN");
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
            try { if (_dvbService != null) _dvbService.LogAdded -= OnLogReceived; } catch { }
            base.OnClosed(e);
        }
    }
}
