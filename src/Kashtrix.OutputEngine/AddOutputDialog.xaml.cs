using System;
using System.Windows;
using System.Windows.Controls;
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
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            BroadcastPlayout.Views.WindowChromeActions.Drag(this, e);

        private void ProtocolComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DestinationTextBox == null || ProtocolComboBox.SelectedItem is not ComboBoxItem item)
                return;

            string tag = item.Tag?.ToString() ?? "DeckLink";
            switch (tag)
            {
                case "DeckLink":
                    DestinationTextBox.Text = "DeckLink SDI Output Device 1";
                    break;
                case "NDI":
                    DestinationTextBox.Text = "Kashtrix-Master-NDI-PGM";
                    break;
                case "Matrox":
                    DestinationTextBox.Text = "Matrox DSX.core Channel A (BNC 1)";
                    break;
                case "AJA":
                    DestinationTextBox.Text = "AJA Kona 5 [Out Port 1 SDI]";
                    break;
                case "UDP_DVB":
                    DestinationTextBox.Text = "udp://239.255.10.1:5000?pkt_size=1316&bitrate=15000000&ttl=32";
                    break;
                case "UDP_UNICAST":
                    DestinationTextBox.Text = "udp://192.168.1.100:5000?pkt_size=1316&bitrate=15000000";
                    break;
                case "SRT":
                    DestinationTextBox.Text = "srt://192.168.1.50:9000?mode=caller&latency=120";
                    break;
                case "RTMP_YouTube":
                    DestinationTextBox.Text = "rtmp://a.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx-xxxx";
                    break;
                case "RTMP_Facebook":
                    DestinationTextBox.Text = "rtmps://live-api-s.facebook.com:443/rtmp/FB-xxxx-xxxx-xxxx";
                    break;
                case "RTMP_Twitch":
                    DestinationTextBox.Text = "rtmp://live.twitch.tv/app/live_xxxxxxxx_xxxxxxxxxx";
                    break;
                case "RTMP_TikTok":
                    DestinationTextBox.Text = "rtmp://push-rtmp-f5-tt.tiktokcdn.com/stage/stream-xxxxxxxxxxxx";
                    break;
                case "RTMP":
                    DestinationTextBox.Text = "rtmp://your-server.com/live/stream_key";
                    break;
                case "HLS":
                    DestinationTextBox.Text = "https://edge-dvb.kashtrix.net/live/master.m3u8";
                    break;
                case "MPD":
                    DestinationTextBox.Text = "https://dash-origin.kashtrix.net/dash/stream.mpd";
                    break;
                case "RTSP":
                    DestinationTextBox.Text = "rtsp://10.0.0.10:8554/live/feed";
                    break;
            }
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

            // Map social RTMP presets and UDP variants to the correct protocol enum
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

            double fps = 50.0;
            if (FpsComboBox.SelectedItem is ComboBoxItem fpsItem)
            {
                double.TryParse(fpsItem.Content?.ToString(), out fps);
            }

            double bitrate = 12.0;
            double.TryParse(BitrateTextBox.Text, out bitrate);

            string resolution = (ResolutionComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1080p50";
            string gpuEncoder = (GpuEncoderComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NVENC H.264 (CUDA)";

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
                RasterFormat = resolution,
                TargetFps = fps > 0 ? fps : 50.0,
                RunningFps = fps > 0 ? fps : 50.0,
                BitrateMbps = bitrate > 0 ? bitrate : 12.0,
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
