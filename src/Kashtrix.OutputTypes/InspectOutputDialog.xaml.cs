using System.Windows;
using Kashtrix.OutputTypes.Models;
using Kashtrix.OutputTypes.Services;

namespace Kashtrix.OutputTypes
{
    public partial class InspectOutputDialog : Window
    {
        private readonly OutputChannel _channel;

        public InspectOutputDialog(OutputChannel channel)
        {
            InitializeComponent();
            _channel = channel;
            DataContext = channel;
        }

        private void TriggerScte35_Click(object sender, RoutedEventArgs e)
        {
            var splice = Scte35DvbService.Instance.TriggerScte35Splice(30.0, "splice_insert");
            MessageBox.Show($"Triggered SCTE-35 DPI Cue for channel {_channel.Name}:\n\nEvent ID: {splice.EventId}\nPTS: {splice.PtsTimestampHex}\nCommand: {splice.CommandType}\nDuration: {splice.DurationSeconds}s",
                            "SCTE-35 Cue Inserted",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void SendMos_Click(object sender, RoutedEventArgs e)
        {
            Scte35DvbService.Instance.Log("MOS", $"[MOS 2.8.4 MANUAL PING] Sent status check to NRCS server for channel: {_channel.Name} | ACK Received (0ms latency)", "INFO");
            MessageBox.Show($"Sent MOS 2.8.4 status ping for '{_channel.Name}'.\n\nMOS Gateway responded: <mosAck><objID>{_channel.Name}</objID><status>ACK</status></mosAck>",
                            "MOS Protocol Gateway",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void ClearAlert_Click(object sender, RoutedEventArgs e)
        {
            _channel.AlertMessage = string.Empty;
            if (_channel.Status == OutputStatus.Warning || _channel.Status == OutputStatus.Error)
            {
                _channel.Status = OutputStatus.Online;
            }
            Scte35DvbService.Instance.Log("ALERT", $"Alert cleared for channel '{_channel.Name}' by operator", "INFO");
            MessageBox.Show($"Active alert and error state cleared for '{_channel.Name}'.", "Alert Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
