using System;
using System.Windows;
using System.Windows.Controls;
using BroadcastPlayout.Professional;

namespace Kashtrix.HAController;

public partial class AddHaPlayoutWindow : Window
{
    public HaHeartbeat? CreatedHeartbeat { get; private set; }

    public AddHaPlayoutWindow()
    {
        InitializeComponent();
        ChannelCombo.ItemsSource = new[] { "CH-01", "CH-02", "CH-03", "KASHTRIX-NEWS", "SPORTS-HD", "BACKUP-POOL" };
        ChannelCombo.SelectedIndex = 0;
        NodeIdBox.Text = $"{Environment.MachineName}-TX0{new Random().Next(2, 9)}";
    }

    private void AddNode_Click(object sender, RoutedEventArgs e)
    {
        var channel = ChannelCombo.Text?.Trim();
        if (string.IsNullOrWhiteSpace(channel))
        {
            BroadcastPlayout.Views.MessageBox.Show("Please enter or select a Channel ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var nodeId = NodeIdBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            BroadcastPlayout.Views.MessageBox.Show("Please enter a Node ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var mode = (ModeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PRIMARY";
        int.TryParse(PriorityBox.Text.Trim(), out var priority);
        if (priority <= 0) priority = mode == "PRIMARY" ? 100 : 90;

        int.TryParse(CapacityBox.Text.Trim(), out var capacity);
        if (capacity <= 0) capacity = 1;

        var armed = ArmedCheck.IsChecked == true;
        var media = MediaBox.Text?.Trim() ?? "Live-Feed.mp4";
        var role = mode == "PRIMARY" ? "ACTIVE" : (mode == "POOL" ? "STANDBY" : "STANDBY");

        CreatedHeartbeat = new HaHeartbeat(
            NodeId: nodeId,
            ChannelId: channel,
            Priority: priority,
            Capacity: capacity,
            ConfiguredMode: mode,
            Role: role,
            OutputArmed: armed,
            CurrentItemId: media,
            PositionSeconds: 12.0,
            UtcTicks: DateTime.UtcNow.Ticks
        );

        DialogResult = true;
        Close();
    }

    private void Header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
