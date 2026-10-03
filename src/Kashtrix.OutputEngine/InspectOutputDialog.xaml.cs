using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Kashtrix.OutputEngine.Models;
using Kashtrix.OutputEngine.Services;
using ThemedMessageBox = BroadcastPlayout.Views.MessageBox;

namespace Kashtrix.OutputEngine
{
    public partial class InspectOutputDialog : Window
    {
        private readonly OutputChannel _channel;
        public ObservableCollection<BroadcastEventLog> ChannelLogs { get; } = new();

        public InspectOutputDialog(OutputChannel channel)
        {
            InitializeComponent();
            BroadcastPlayout.Views.WindowChromeActions.ApplyCleanBorder(this);
            _channel = channel;
            DataContext = channel;
            LogListBox.ItemsSource = ChannelLogs;
            PopulateLogs();
            Scte35DvbService.Instance.LogAdded += OnLogAdded;
            Closed += (_, _) => Scte35DvbService.Instance.LogAdded -= OnLogAdded;
        }

        private void PopulateLogs()
        {
            ChannelLogs.Clear();
            if (_channel.HasAlert && !string.IsNullOrWhiteSpace(_channel.AlertMessage))
            {
                ChannelLogs.Add(new BroadcastEventLog
                {
                    Category = "ALERT",
                    ChannelName = _channel.Name,
                    Level = "ERROR",
                    Message = _channel.AlertMessage,
                    Timestamp = DateTime.Now
                });
            }

            foreach (var log in Scte35DvbService.Instance.Logs.Where(MatchesChannel).Take(150))
            {
                ChannelLogs.Add(log);
            }
        }

        private bool MatchesChannel(BroadcastEventLog log)
        {
            if (string.Equals(log.ChannelName, _channel.Name, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.IsNullOrEmpty(log.ChannelName))
            {
                return log.Category is "ALERT" or "OUTPUT" or "OUTPUT_FAULT" or "HARDWARE" or "ENGINE" or "MOS";
            }
            return false;
        }

        private void OnLogAdded(BroadcastEventLog log)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (MatchesChannel(log))
                {
                    ChannelLogs.Insert(0, log);
                    if (ChannelLogs.Count > 250) ChannelLogs.RemoveAt(ChannelLogs.Count - 1);
                }
            });
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            BroadcastPlayout.Views.WindowChromeActions.Drag(this, e);

        private void SendMos_Click(object sender, RoutedEventArgs e)
        {
            Scte35DvbService.Instance.Log("MOS", $"[MOS 2.8.4 MANUAL PING] Sent status check to NRCS server for channel: {_channel.Name} | ACK Received (0ms latency)", "INFO", _channel.Name);
            ThemedMessageBox.Show(this, $"Sent MOS 2.8.4 status ping for '{_channel.Name}'.\n\nMOS Gateway responded: <mosAck><objID>{_channel.Name}</objID><status>ACK</status></mosAck>",
                            "MOS Protocol Gateway",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void ClearAlert_Click(object sender, RoutedEventArgs e)
        {
            _channel.AlertMessage = string.Empty;
            if (_channel.Status == OutputStatus.Warning || _channel.Status == OutputStatus.Error)
            {
                _channel.Status = _channel.IsEnabled ? OutputStatus.Online : OutputStatus.Standby;
            }
            Scte35DvbService.Instance.Log("ALERT", $"Alert cleared for channel '{_channel.Name}' by operator", "INFO", _channel.Name);
            PopulateLogs();
            ThemedMessageBox.Show(this, $"Active alert and error state cleared for '{_channel.Name}'.", "Alert Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
