using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BroadcastPlayout.Services;

namespace Kashtrix.NRCS;

public partial class NepaliCalendarDialog : Window
{
    private readonly NrcsPlatformStore _store;
    private int _currentBsYear;
    private int _currentBsMonth;
    private int _selectedBsDay;
    private bool _suppressEvents;

    public DateTime SelectedAdDate { get; private set; } = DateTime.Today;
    public NepaliDate SelectedBsDate { get; private set; }
    public string DateSystem => _store.GetSetting("DateSystem", "BS");
    public string SelectedFormattedDate => $"{SelectedBsDate.Year}-{SelectedBsDate.Month:00}-{SelectedBsDate.Day:00} BS ({SelectedAdDate:yyyy-MM-dd} AD)";

    public NepaliCalendarDialog(NrcsPlatformStore store, DateTime? initialAdDate = null)
    {
        InitializeComponent();
        _store = store;

        var initial = initialAdDate ?? DateTime.Today;
        SelectedAdDate = initial;
        SelectedBsDate = NepaliCalendarService.ConvertToBs(initial);

        _currentBsYear = SelectedBsDate.Year;
        _currentBsMonth = SelectedBsDate.Month;
        _selectedBsDay = SelectedBsDate.Day;

        InitializeYearsAndMonths();
        LoadCurrentSettings();
        UpdateTodayBanner();
        RenderCalendarGrid();

        AdConvertPicker.SelectedDate = initial;
    }

    private void InitializeYearsAndMonths()
    {
        _suppressEvents = true;
        try
        {
            BsYearBox.Items.Clear();
            for (var y = 2000; y <= 2099; y++)
            {
                BsYearBox.Items.Add(y);
            }
            BsYearBox.SelectedItem = _currentBsYear;

            BsMonthBox.Items.Clear();
            for (var m = 0; m < 12; m++)
            {
                var en = NepaliCalendarService.MonthsEn[m];
                var np = NepaliCalendarService.MonthsNp[m];
                BsMonthBox.Items.Add($"{m + 1}: {np} ({en})");
            }
            BsMonthBox.SelectedIndex = Math.Clamp(_currentBsMonth - 1, 0, 11);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void LoadCurrentSettings()
    {
        var ds = _store.GetSetting("DateSystem", "BS").ToUpperInvariant();
        foreach (ComboBoxItem item in DateSystemBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), ds, StringComparison.OrdinalIgnoreCase))
            {
                DateSystemBox.SelectedItem = item;
                break;
            }
        }

        var num = _store.GetSetting("NumeralSystem", "Devanagari");
        foreach (ComboBoxItem item in NumeralSystemBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), num, StringComparison.OrdinalIgnoreCase))
            {
                NumeralSystemBox.SelectedItem = item;
                break;
            }
        }
    }

    private void UpdateTodayBanner()
    {
        var today = DateTime.Today;
        var bsToday = NepaliCalendarService.ConvertToBs(today);
        TodayBsText.Text = $"{NepaliCalendarService.ToNepaliDigits(bsToday.Year.ToString())} {bsToday.MonthNameNp} {NepaliCalendarService.ToNepaliDigits(bsToday.Day.ToString())}, {bsToday.DayNameNp}";
        TodayAdText.Text = $"Gregorian AD: {today:yyyy-MM-dd} ({today:dddd}) · BS {bsToday.Year:0000}-{bsToday.Month:00}-{bsToday.Day:00}";
    }

    private void RenderCalendarGrid()
    {
        CalendarDaysGrid.Children.Clear();

        var firstDayAd = NepaliCalendarService.ConvertBsToAd(_currentBsYear, _currentBsMonth, 1);
        var startDayOfWeek = (int)firstDayAd.DayOfWeek; // 0 = Sunday
        var totalDays = NepaliCalendarService.GetBsDaysInMonth(_currentBsYear, _currentBsMonth);

        var todayBs = NepaliCalendarService.ConvertToBs(DateTime.Today);

        // Pre-fill leading empty days
        for (var i = 0; i < startDayOfWeek; i++)
        {
            var empty = new Border { Background = Brushes.Transparent };
            CalendarDaysGrid.Children.Add(empty);
        }

        // Fill month days
        for (var d = 1; d <= totalDays; d++)
        {
            var dayNum = d;
            var dayAd = NepaliCalendarService.ConvertBsToAd(_currentBsYear, _currentBsMonth, dayNum);
            var isSaturday = dayAd.DayOfWeek == DayOfWeek.Saturday;
            var isToday = (todayBs.Year == _currentBsYear && todayBs.Month == _currentBsMonth && todayBs.Day == dayNum);
            var isSelected = (SelectedBsDate.Year == _currentBsYear && SelectedBsDate.Month == _currentBsMonth && _selectedBsDay == dayNum);

            var btn = new Button
            {
                Margin = new Thickness(1),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(88, 166, 255)) : (isToday ? new SolidColorBrush(Color.FromRgb(35, 134, 54)) : new SolidColorBrush(Color.FromRgb(48, 54, 61))),
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(31, 111, 235)) : (isToday ? new SolidColorBrush(Color.FromArgb(40, 35, 134, 54)) : new SolidColorBrush(Color.FromRgb(22, 27, 34)))
            };

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            var nepDigit = NepaliCalendarService.ToNepaliDigits(dayNum.ToString());
            var numText = new TextBlock
            {
                Text = nepDigit,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = isSaturday ? new SolidColorBrush(Color.FromRgb(255, 123, 114)) : Brushes.White
            };
            var enText = new TextBlock
            {
                Text = $"{dayNum} ({dayAd.Day})",
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
            };
            stack.Children.Add(numText);
            stack.Children.Add(enText);
            btn.Content = stack;

            btn.Click += (_, _) =>
            {
                _selectedBsDay = dayNum;
                SelectedAdDate = dayAd;
                SelectedBsDate = NepaliCalendarService.ConvertToBs(dayAd);
                StatusLabel.Text = $"Selected: BS {SelectedBsDate.Year:0000}-{SelectedBsDate.Month:00}-{SelectedBsDate.Day:00} ({SelectedBsDate.MonthNameNp}) ➔ AD {SelectedAdDate:yyyy-MM-dd}";
                RenderCalendarGrid();
            };

            CalendarDaysGrid.Children.Add(btn);
        }

        // Fill trailing empty cells to complete the 42 cells grid
        var filled = startDayOfWeek + totalDays;
        for (var i = filled; i < 42; i++)
        {
            CalendarDaysGrid.Children.Add(new Border { Background = Brushes.Transparent });
        }
    }

    private void BsYearMonth_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (BsYearBox.SelectedItem is int y) _currentBsYear = y;
        if (BsMonthBox.SelectedIndex >= 0) _currentBsMonth = BsMonthBox.SelectedIndex + 1;
        RenderCalendarGrid();
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentBsMonth--;
        if (_currentBsMonth < 1)
        {
            _currentBsMonth = 12;
            _currentBsYear = Math.Max(2000, _currentBsYear - 1);
        }
        UpdateDropdowns();
        RenderCalendarGrid();
    }

    private void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _currentBsMonth++;
        if (_currentBsMonth > 12)
        {
            _currentBsMonth = 1;
            _currentBsYear = Math.Min(2099, _currentBsYear + 1);
        }
        UpdateDropdowns();
        RenderCalendarGrid();
    }

    private void UpdateDropdowns()
    {
        _suppressEvents = true;
        try
        {
            BsYearBox.SelectedItem = _currentBsYear;
            BsMonthBox.SelectedIndex = Math.Clamp(_currentBsMonth - 1, 0, 11);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void Today_Click(object sender, RoutedEventArgs e)
    {
        var today = DateTime.Today;
        SelectedAdDate = today;
        SelectedBsDate = NepaliCalendarService.ConvertToBs(today);
        _currentBsYear = SelectedBsDate.Year;
        _currentBsMonth = SelectedBsDate.Month;
        _selectedBsDay = SelectedBsDate.Day;
        UpdateDropdowns();
        RenderCalendarGrid();
        StatusLabel.Text = $"Jumped to today: {SelectedBsDate}";
    }

    private void AdConvertPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AdConvertPicker.SelectedDate is DateTime dt)
        {
            var bs = NepaliCalendarService.ConvertToBs(dt);
            var dev = $"{NepaliCalendarService.ToNepaliDigits(bs.Year.ToString())}-{NepaliCalendarService.ToNepaliDigits(bs.Month.ToString("00"))}-{NepaliCalendarService.ToNepaliDigits(bs.Day.ToString("00"))}";
            AdToBsResultBox.Text = $"{bs.Year:0000}-{bs.Month:00}-{bs.Day:00} BS ({dev}) · {bs.MonthNameNp} {bs.DayNameNp}";
        }
    }

    private void ConvertBsToAd_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ConvBsYearBox.Text, out var y) &&
            int.TryParse(ConvBsMonthBox.Text, out var m) &&
            int.TryParse(ConvBsDayBox.Text, out var d))
        {
            try
            {
                var ad = NepaliCalendarService.ConvertBsToAd(y, m, d);
                BsToAdResultBox.Text = $"{ad:yyyy-MM-dd} AD ({ad:dddd})";
            }
            catch (Exception ex)
            {
                BsToAdResultBox.Text = "Conversion error: " + ex.Message;
            }
        }
        else
        {
            BsToAdResultBox.Text = "Please enter valid numeric Year, Month, Day.";
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var ds = (DateSystemBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "BS";
        var num = (NumeralSystemBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Devanagari";

        _store.SetSetting("DateSystem", ds);
        _store.SetSetting("NumeralSystem", num);

        StatusLabel.Text = $"NRCS Date System saved: {ds} ({num} numerals). Applying across all modules...";
        MessageBox.Show($"Date system updated to '{ds}' with '{num}' numerals.\nStory Bank, Rundown, and Planning Diary will now format using this system.", "NRCS Calendar Setting", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyDate_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
