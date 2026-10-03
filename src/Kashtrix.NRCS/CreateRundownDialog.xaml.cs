using System.Windows;
using System.Windows.Controls;
using BroadcastPlayout.Services;

namespace Kashtrix.NRCS;

public partial class CreateRundownDialog : Window
{
    private readonly NrcsPlatformStore _store;
    private readonly List<NrcsProgram> _programs;

    public NrcsRundown? CreatedRundown { get; private set; }

    public CreateRundownDialog(NrcsPlatformStore store)
    {
        InitializeComponent();
        _store = store;

        AirDatePicker.SelectedDate = DateTime.Today;
        AirTimeBox.Text = DateTime.Now.ToString("HH:mm");

        _programs = _store.ListPrograms().Where(x => x.IsActive).OrderBy(x => x.Name).ToList();

        ProgramBox.Items.Clear();
        ProgramBox.Items.Add(new ComboBoxItem { Content = "-- Custom / Standalone Rundown (No Preset) --", Tag = "" });
        foreach (var p in _programs)
        {
            ProgramBox.Items.Add(new ComboBoxItem { Content = $"{p.Name} ({p.Category} · {p.DefaultDurationMinutes} min)", Tag = p.Id });
        }
        ProgramBox.SelectedIndex = _programs.Count > 0 ? 1 : 0;

        UpdateNepaliDatePreview();
        UpdateDefaultName();
    }

    private void ProgramBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateDefaultName();
        var selectedProg = GetSelectedProgram();
        if (selectedProg != null)
        {
            ChannelBox.Text = !string.IsNullOrWhiteSpace(selectedProg.ChannelId) ? selectedProg.ChannelId : "KTX-PLAYOUT-01";
        }
    }

    private NrcsProgram? GetSelectedProgram()
    {
        var tag = (ProgramBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return string.IsNullOrEmpty(tag) ? null : _programs.FirstOrDefault(x => x.Id == tag);
    }

    private void AirDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateNepaliDatePreview();
        UpdateDefaultName();
    }

    private void UpdateNepaliDatePreview()
    {
        var dt = AirDatePicker.SelectedDate ?? DateTime.Today;
        var bs = NepaliCalendarService.ConvertToBs(dt);
        NepaliDatePreviewText.Text = $"{NepaliCalendarService.ToNepaliDigits(bs.Year.ToString())} {bs.MonthNameNp} {NepaliCalendarService.ToNepaliDigits(bs.Day.ToString())}, {bs.DayNameNp} · BS {bs.Year:0000}-{bs.Month:00}-{bs.Day:00}";
    }

    private void UpdateDefaultName()
    {
        var prog = GetSelectedProgram();
        var dt = AirDatePicker.SelectedDate ?? DateTime.Today;
        var bs = NepaliCalendarService.ConvertToBs(dt);
        var baseName = prog != null ? prog.Name : "NEWS BULLETIN";
        RundownNameBox.Text = $"{baseName} - {bs.Year:0000}/{bs.Month:00}/{bs.Day:00}";
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = RundownNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorText.Text = "Please enter a rundown name.";
            return;
        }

        var date = AirDatePicker.SelectedDate ?? DateTime.Today;
        var timeStr = AirTimeBox.Text?.Trim() ?? "19:00";
        if (TimeSpan.TryParse(timeStr, out var time))
        {
            date = date.Date + time;
        }

        var prog = GetSelectedProgram();
        var channel = ChannelBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(channel)) channel = prog?.ChannelId ?? "KTX-PLAYOUT-01";

        var autoOpenClose = AutoOpenCloseCheck.IsChecked == true;
        var autoBreaks = AutoBreaksCheck.IsChecked == true;

        try
        {
            CreatedRundown = _store.CreateRundown(name, channel, date.ToUniversalTime(), prog?.Id, autoOpenClose, autoBreaks);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Failed to create rundown: " + ex.Message;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
