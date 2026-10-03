using System.Windows;
using System.Windows.Controls;
using BroadcastPlayout.Services;

namespace Kashtrix.NRCS;

public partial class AddBreakDialog : Window
{
    private readonly NrcsPlatformStore _store;
    private readonly string? _programId;
    private readonly List<NrcsBreakTemplate> _templates;

    public string ItemTitle { get; private set; } = "";
    public string ItemType { get; private set; } = "BREAK";
    public string BreakTitle => ItemTitle;
    public string BreakType => ItemType;
    public int DurationSeconds { get; private set; } = 60;
    public string Segment { get; private set; } = "A";
    public string Notes { get; private set; } = "";

    public AddBreakDialog(NrcsPlatformStore store, string? programId, bool isSegmentHeader = false)
    {
        InitializeComponent();
        _store = store;
        _programId = programId;

        _templates = _store.ListBreakTemplates(_programId).ToList();

        PresetBox.Items.Clear();
        PresetBox.Items.Add(new ComboBoxItem { Content = "-- Custom Entry --", Tag = null });
        foreach (var t in _templates)
        {
            var pfx = string.IsNullOrEmpty(t.ProgramId) ? "[GLOBAL]" : "[PROGRAM]";
            PresetBox.Items.Add(new ComboBoxItem { Content = $"{pfx} {t.Title} ({t.BreakType} · {t.DefaultDurationSeconds}s · Seg {t.Segment})", Tag = t });
        }
        PresetBox.SelectedIndex = 0;

        if (isSegmentHeader)
        {
            DialogTitleText.Text = "➕ INSERT SEGMENT HEADER";
            BreakTypeBox.SelectedIndex = 1; // SEGMENT
            DurationBox.Text = "0";
            TitleBox.Text = "International News";
            SegmentBox.Text = "B";
        }
        else
        {
            BreakTypeBox.SelectedIndex = 0; // COMMERCIAL
            DurationBox.Text = "60";
            TitleBox.Text = "Commercial Break";
            SegmentBox.Text = "A";
        }
    }

    private void PresetBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetBox.SelectedItem is ComboBoxItem item && item.Tag is NrcsBreakTemplate t)
        {
            TitleBox.Text = t.Title;
            DurationBox.Text = t.DefaultDurationSeconds.ToString();
            SegmentBox.Text = t.Segment;
            NotesBox.Text = t.Notes;

            if (t.BreakType.Equals("SEGMENT", StringComparison.OrdinalIgnoreCase))
                BreakTypeBox.SelectedIndex = 1;
            else if (t.BreakType.Equals("SPONSOR", StringComparison.OrdinalIgnoreCase))
                BreakTypeBox.SelectedIndex = 2;
            else if (t.BreakType.Equals("STATION_ID", StringComparison.OrdinalIgnoreCase))
                BreakTypeBox.SelectedIndex = 3;
            else
                BreakTypeBox.SelectedIndex = 0;
        }
    }

    private void BreakTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = (BreakTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "BREAK";
        if (tag == "SEGMENT")
        {
            DurationBox.Text = "0";
            if (string.IsNullOrWhiteSpace(TitleBox.Text) || TitleBox.Text.Contains("Break"))
                TitleBox.Text = "International News";
        }
    }

    private void Insert_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorText.Text = "Please enter a title.";
            return;
        }

        var tag = (BreakTypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "BREAK";
        var dur = int.TryParse(DurationBox.Text, out var d) ? Math.Max(0, d) : 0;
        var seg = string.IsNullOrWhiteSpace(SegmentBox.Text) ? "A" : SegmentBox.Text.Trim();

        ItemTitle = title;
        ItemType = tag;
        DurationSeconds = dur;
        Segment = seg;
        Notes = NotesBox.Text?.Trim() ?? "";

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
