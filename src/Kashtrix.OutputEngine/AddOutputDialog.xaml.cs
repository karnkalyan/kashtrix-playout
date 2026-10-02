using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BroadcastPlayout.Outputs;
using Kashtrix.OutputEngine.Models;
using ThemedMessageBox = BroadcastPlayout.Views.MessageBox;

namespace Kashtrix.OutputEngine
{
    public partial class AddOutputDialog : Window
    {
        public OutputChannel? ResultChannel { get; private set; }

        public AddOutputDialog()
        {
            InitializeComponent();
            BroadcastPlayout.Views.WindowChromeActions.ApplyCleanBorder(this);
            Loaded += (_, _) => UpdateHardwareOptions();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            BroadcastPlayout.Views.WindowChromeActions.Drag(this, e);

        private void ProtocolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationTextBox == null || ProtocolComboBox.SelectedItem is not ComboBoxItem item)
                return;

            string tag = item.Tag?.ToString() ?? "DeckLink";
            bool isCard = tag is "DeckLink" or "Matrox" or "AJA";

            if (GpuEncoderComboBox != null && CardHardwareNotice != null)
            {
                GpuEncoderComboBox.Visibility = isCard ? Visibility.Collapsed : Visibility.Visible;
                CardHardwareNotice.Visibility = isCard ? Visibility.Visible : Visibility.Collapsed;
            }

            switch (tag)
            {
                case "DeckLink":
                    DestinationTextBox.Text = "DeckLink SDI Output Device 1";
                    BitrateTextBox.Text = "1500.0";
                    break;
                case "NDI":
                    DestinationTextBox.Text = "Kashtrix-Master-NDI-PGM";
                    BitrateTextBox.Text = "125.0";
                    break;
                case "Matrox":
                    DestinationTextBox.Text = "Matrox DSX.core Channel A (BNC 1)";
                    BitrateTextBox.Text = "1500.0";
                    break;
                case "AJA":
                    DestinationTextBox.Text = "AJA Kona 5 [Out Port 1 SDI]";
                    BitrateTextBox.Text = "1500.0";
                    break;
                case "UDP_DVB":
                    DestinationTextBox.Text = "udp://239.255.10.1:5000?pkt_size=1316&bitrate=15000000&ttl=32";
                    BitrateTextBox.Text = "15.0";
                    break;
                case "UDP_UNICAST":
                    DestinationTextBox.Text = "udp://192.168.1.100:5000?pkt_size=1316&bitrate=15000000";
                    BitrateTextBox.Text = "15.0";
                    break;
                case "SRT":
                    DestinationTextBox.Text = "srt://192.168.1.50:9000?mode=caller&latency=120";
                    BitrateTextBox.Text = "8.5";
                    break;
                case "RTMP_YouTube":
                    DestinationTextBox.Text = "rtmp://a.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx-xxxx";
                    BitrateTextBox.Text = "6.0";
                    break;
                case "RTMP_Facebook":
                    DestinationTextBox.Text = "rtmps://live-api-s.facebook.com:443/rtmp/FB-xxxx-xxxx-xxxx";
                    BitrateTextBox.Text = "4.5";
                    break;
                case "RTMP_Twitch":
                    DestinationTextBox.Text = "rtmp://live.twitch.tv/app/live_xxxxxxxx_xxxxxxxxxx";
                    BitrateTextBox.Text = "6.0";
                    break;
                case "RTMP_TikTok":
                    DestinationTextBox.Text = "rtmp://push-rtmp-f5-tt.tiktokcdn.com/stage/stream-xxxxxxxxxxxx";
                    BitrateTextBox.Text = "4.0";
                    break;
                case "RTMP":
                    DestinationTextBox.Text = "rtmp://your-server.com/live/stream_key";
                    BitrateTextBox.Text = "6.0";
                    break;
                case "HLS":
                    DestinationTextBox.Text = "https://edge-dvb.kashtrix.net/live/master.m3u8";
                    BitrateTextBox.Text = "7.5";
                    break;
                case "MPD":
                    DestinationTextBox.Text = "https://dash-origin.kashtrix.net/dash/stream.mpd";
                    BitrateTextBox.Text = "7.0";
                    break;
                case "RTSP":
                    DestinationTextBox.Text = "rtsp://10.0.0.10:8554/live/feed";
                    BitrateTextBox.Text = "8.0";
                    break;
            }

            UpdateHardwareOptions();
        }

        private void UpdateHardwareOptions()
        {
            if (ResolutionComboBox == null || ProtocolComboBox == null) return;
            var item = ProtocolComboBox.SelectedItem as ComboBoxItem;
            string tag = item?.Tag?.ToString() ?? "DeckLink";

            if (tag is "DeckLink")
            {
                // Query hardware display modes dynamically from card
                var hardwareModes = DeckLinkOutputAdapter.EnumerateDisplayModes(0);
                var existingItems = new List<string> { "Auto Detect (From Card Hardware)" };

                if (hardwareModes != null && hardwareModes.Count > 0)
                {
                    foreach (var m in hardwareModes)
                    {
                        var modeLabel = $"{m.ModeName} ({m.Width}x{m.Height} @ {m.FrameRate:F2} fps - {m.ScanMode})";
                        if (!existingItems.Contains(modeLabel))
                            existingItems.Add(modeLabel);
                    }
                }

                existingItems.Add("1080p50 (1920x1080 @ 50.00 fps)");
                existingItems.Add("1080i50 (Broadcast standard 25.00 fps)");
                existingItems.Add("1080p59.94 (1920x1080 @ 59.94 fps)");
                existingItems.Add("1080p60 (1920x1080 @ 60.00 fps)");
                existingItems.Add("2160p50 (4K UHD 3840x2160 @ 50.00 fps)");
                existingItems.Add("2160p59.94 (4K UHD 3840x2160 @ 59.94 fps)");
                existingItems.Add("720p50 (HD 1280x720 @ 50.00 fps)");
                existingItems.Add("Custom (User Defined)...");

                ResolutionComboBox.Items.Clear();
                foreach (var label in existingItems.Distinct())
                {
                    ResolutionComboBox.Items.Add(new ComboBoxItem { Content = label });
                }
                ResolutionComboBox.SelectedIndex = 0;
            }
        }

        private void ResolutionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomResolutionPanel == null) return;
            string sel = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            bool isCustom = sel.StartsWith("Custom", StringComparison.OrdinalIgnoreCase);
            bool isFpsCustom = ((FpsComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty).StartsWith("Custom", StringComparison.OrdinalIgnoreCase);
            CustomResolutionPanel.Visibility = (isCustom || isFpsCustom) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FpsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CustomResolutionPanel == null) return;
            string selFps = (FpsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
            bool isFpsCustom = selFps.StartsWith("Custom", StringComparison.OrdinalIgnoreCase);
            string selRes = ((ResolutionComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty);
            bool isResCustom = selRes.StartsWith("Custom", StringComparison.OrdinalIgnoreCase);
            CustomResolutionPanel.Visibility = (isFpsCustom || isResCustom) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ThemedMessageBox.Show(this, "Please enter an output feed name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = ProtocolComboBox.SelectedItem as ComboBoxItem;
            string tag = item?.Tag?.ToString() ?? "DeckLink";

            BroadcastOutputProtocol protocol;
            string streamPreset = string.Empty;
            switch (tag)
            {
                case "UDP_DVB":
                case "UDP_UNICAST":
                    protocol = BroadcastOutputProtocol.DvbUdp;
                    break;
                case "RTMP_YouTube":
                    protocol = BroadcastOutputProtocol.RTMP;
                    streamPreset = "YouTube Live";
                    break;
                case "RTMP_Facebook":
                    protocol = BroadcastOutputProtocol.RTMP;
                    streamPreset = "Facebook Live";
                    break;
                case "RTMP_Twitch":
                    protocol = BroadcastOutputProtocol.RTMP;
                    streamPreset = "Twitch Live";
                    break;
                case "RTMP_TikTok":
                    protocol = BroadcastOutputProtocol.RTMP;
                    streamPreset = "TikTok Live";
                    break;
                default:
                    if (!Enum.TryParse(tag, out protocol))
                        protocol = BroadcastOutputProtocol.DeckLink;
                    break;
            }

            bool isCard = protocol is BroadcastOutputProtocol.DeckLink or BroadcastOutputProtocol.Matrox or BroadcastOutputProtocol.AJA;

            string resolutionStr = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1080p50";
            bool isCustomRes = resolutionStr.StartsWith("Custom", StringComparison.OrdinalIgnoreCase);

            double fps = 50.0;
            string fpsStr = (FpsComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "50.00";
            if (fpsStr.StartsWith("Custom", StringComparison.OrdinalIgnoreCase))
            {
                double.TryParse(CustomFpsTextBox?.Text, out fps);
            }
            else
            {
                double.TryParse(fpsStr, out fps);
            }
            if (fps <= 0) fps = 50.0;

            int customW = 1920;
            int customH = 1080;
            if (isCustomRes)
            {
                int.TryParse(CustomWidthTextBox?.Text, out customW);
                int.TryParse(CustomHeightTextBox?.Text, out customH);
                if (customW <= 0) customW = 1920;
                if (customH <= 0) customH = 1080;
                resolutionStr = $"{customW}x{customH} (Custom)";
            }

            double bitrate = 12.0;
            double.TryParse(BitrateTextBox.Text, out bitrate);
            if (bitrate <= 0) bitrate = isCard ? 1500.0 : 12.0;

            string gpuEncoder = isCard
                ? "Direct Uncompressed (Raw PCIe DMA)"
                : ((GpuEncoderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NVENC H.264 (CUDA)");

            int serviceId = 101;
            if (ServiceIdTextBox != null) int.TryParse(ServiceIdTextBox.Text, out serviceId);
            int pmtPid = 256;
            if (PmtPidTextBox != null) int.TryParse(PmtPidTextBox.Text, out pmtPid);
            int videoPid = 257;
            if (VideoPidTextBox != null) int.TryParse(VideoPidTextBox.Text, out videoPid);
            int audioPid = 258;
            if (AudioPidTextBox != null) int.TryParse(AudioPidTextBox.Text, out audioPid);
            int pcrPid = 257;
            if (PcrPidTextBox != null) int.TryParse(PcrPidTextBox.Text, out pcrPid);

            ResultChannel = new OutputChannel
            {
                Name = NameTextBox.Text.Trim(),
                Protocol = protocol,
                DestinationUri = DestinationTextBox.Text.Trim(),
                RasterFormat = resolutionStr,
                IsCustomResolution = isCustomRes,
                CustomWidth = customW,
                CustomHeight = customH,
                TargetFps = fps,
                RunningFps = fps,
                BitrateMbps = bitrate,
                Status = OutputStatus.Online,
                IsEnabled = true,
                AlertMessage = string.Empty,
                PcrJitterNs = 2.4,
                LatencyMs = 2.8,
                BufferPercent = 95,
                GpuEncoder = gpuEncoder,
                StreamPreset = streamPreset,
                ServiceId = serviceId > 0 ? serviceId : 101,
                PmtPid = pmtPid > 0 ? pmtPid : 256,
                VideoPid = videoPid > 0 ? videoPid : 257,
                AudioPid = audioPid > 0 ? audioPid : 258,
                PcrPid = pcrPid > 0 ? pcrPid : 257,
                DvbStandardEnabled = DvbCompliantCheckBox?.IsChecked == true,
                Scte35Enabled = Scte35CheckBox?.IsChecked == true
            };

            if (!Services.BroadcastOutputEngine.TryValidateOutput(ResultChannel, out var validationError))
            {
                ResultChannel = null;
                ThemedMessageBox.Show(this, validationError, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
