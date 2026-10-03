using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Text.Json;
using System.IO;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using BroadcastPlayout.Services;
using BroadcastPlayout.Models;
using BroadcastPlayout.Views;
using Microsoft.Win32;
using Kashtrix.NRCS.Services;

namespace Kashtrix.NRCS;

public sealed class FontItem
{
    public string Name { get; init; } = "";
    public FontFamily Family { get; init; } = null!;
    public override string ToString() => Name;
}

public partial class MainWindow : Window
{
    private readonly NrcsPlatformStore _store = new();
    private readonly BroadcastDatabase _database = new();
    private readonly DispatcherTimer _liveRefresh = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly PlanningDiaryService _diaryService = PlanningDiaryService.Instance;
    private readonly WireIngestService _wireService = WireIngestService.Instance;
    private readonly ConnectorManager _connectorManager = ConnectorManager.Instance;

    private readonly List<FontItem> _allFontItems = [];
    private NrcsProgram? _selectedProgram;
    private NrcsBreakTemplate? _selectedBreakPreset;
    private NrcsCategory? _selectedCategory;

    private NrcsRundown? _rundown;
    private NrcsStory? _story;
    private NrcsStory? _rundownStory;
    private NrcsAssignment? _assignment;
    private NrcsStoryVersion? _selectedVersion;
    private PlanningEvent? _selectedEvent;
    private WireArticle? _selectedWire;
    private bool _loading;
    private bool _suppressFormatEvents;
    private List<CgProject> _cgCatalog = [];

    public MainWindow()
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);
        InitializeFontFamilies();

        AssignmentStatusBox.ItemsSource = new[] { "PLANNED", "ASSIGNED", "IN PROGRESS", "READY", "HOLD", "DONE", "CANCELLED" };
        AssignmentPriorityBox.ItemsSource = new[] { "LOW", "NORMAL", "HIGH", "URGENT", "BREAKING" };
        StoryStatusBox.ItemsSource = new[] { "DRAFT", "IN PROGRESS", "REVIEW", "APPROVED", "READY", "ON AIR", "COMPLETE" };
        StoryPriorityBox.ItemsSource = new[] { "LOW", "NORMAL", "HIGH", "URGENT", "BREAKING" };
        StoryTypeBox.ItemsSource = new[] { "ANCHOR", "PKG", "VO", "SOT", "LIVE", "REMOTE", "CG", "BREAK", "SEGMENT", "OPENING", "CLOSING", "PROMO", "FILLER" };
        StartModeBox.ItemsSource = new[] { "FOLLOW", "HARD", "MANUAL", "JIP", "HOLD" };
        ItemStatusBox.ItemsSource = new[] { "READY", "PENDING", "PLAYED", "KILLED" };
        AssignmentFilterModeBox.ItemsSource = new[] { "ALL", "DAY", "NEXT 7 DAYS", "OVERDUE", "OPEN ONLY" };
        AssignmentFilterModeBox.SelectedItem = "ALL";
        AssignmentDateFilter.SelectedDate = DateTime.Today;
        DbText.Text = NrcsPlatformStore.DatabasePath;
        CgLayerBox.ItemsSource = Enumerable.Range(0, 100).Select(x => x.ToString()).ToArray();

        RefreshCgCatalog();
        UpdateLiveCalendarDate();
        RefreshCategories();
        RefreshPrograms();
        RefreshBreakPresets();

        _liveRefresh.Tick += (_, _) =>
        {
            RefreshLiveOnly();
            UpdateLiveCalendarDate();
        };
        _liveRefresh.Start();
        RefreshAll();
        RefreshDiary();
        RefreshWires();
        RefreshConnectorHealth();
    }

    private void RefreshCgCatalog()
    {
        try
        {
            var db = new BroadcastDatabase();
            _cgCatalog = db.LoadState("cg-projects", new List<CgProject>());
            if (_cgCatalog.Count == 0) _cgCatalog = CgDemoFactory.CreateDefaults();
            _cgCatalog = _cgCatalog.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            CgTemplateBox.ItemsSource = _cgCatalog.Select(x => x.Name).ToArray();
            UpdateCgTemplateInfo(CgTemplateBox.Text);
        }
        catch
        {
            _cgCatalog = CgDemoFactory.CreateDefaults().OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            CgTemplateBox.ItemsSource = _cgCatalog.Select(x => x.Name).ToArray();
        }
    }

    private void CgTemplateBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        Dispatcher.BeginInvoke(new Action(() => UpdateCgTemplateInfo(CgTemplateBox.Text)));
    }

    private void UpdateCgTemplateInfo(string? name)
    {
        var project = _cgCatalog.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (project is null)
        {
            CgTemplateInfoText.Text = "Choose from the canonical 200-template catalog, or type an exact project name.";
            return;
        }
        var textLayers = project.Layers.Count(x => x.Type.Equals("Text", StringComparison.OrdinalIgnoreCase));
        var animated = project.Layers.Count(x => x.Keyframes.Count > 0 || !x.AnimationIn.Equals("None", StringComparison.OrdinalIgnoreCase));
        CgTemplateInfoText.Text = $"{project.Layers.Count} layers · {textLayers} text · {animated} animated · {project.DurationSeconds:0.#} sec · ID {project.Id.ToString()[..8]}";
    }

    private void RefreshAll()
    {
        _loading = true;
        try
        {
            RefreshRundowns();
            RefreshStoryBank();
            RefreshAssignments();
            RefreshRundownStories();
            RefreshCgCatalog();
            RefreshDashboard();
            AssignmentStoryBox.ItemsSource = _store.ListStoryBank();
        }
        finally { _loading = false; }
    }

    private void RefreshDashboard()
    {
        var rundowns = _store.ListRundowns();
        DashboardRundownGrid.ItemsSource = rundowns;
        DashRundownCountText.Text = rundowns.Count.ToString();
        var allStories = _store.ListStoryBank();
        var readyCount = allStories.Count(x => x.Status is "READY" or "APPROVED");
        DashStoriesReadyText.Text = readyCount.ToString();

        var assignments = _store.ListAssignments();
        var openAssignments = assignments.Count(x => !IsClosedAssignment(x.Status));
        DashAssignmentsText.Text = openAssignments.ToString();

        QuickStatsText.Text = $"Rundowns: {rundowns.Count} · Bank: {allStories.Count} · Ready: {readyCount}";
    }

    private async void RefreshConnectorHealth()
    {
        try
        {
            var reports = await _connectorManager.CheckAllHealthAsync();
            ConnectorStatusGrid.ItemsSource = reports;
            DashHealthStatusText.Text = reports.All(x => x.Status == ConnectorStatus.Healthy || x.Status == ConnectorStatus.Connected) ? "OPTIMAL" : "WARNING";
        }
        catch { }
    }

    private void RefreshRundowns()
    {
        var rows = _store.ListRundowns();
        RundownList.ItemsSource = rows;
        if (_rundown is not null) RundownList.SelectedItem = rows.FirstOrDefault(x => x.Id == _rundown.Id);
        if (RundownList.SelectedItem is null && rows.Count > 0)
        {
            _rundown = rows[0];
            RundownList.SelectedItem = _rundown;
        }
    }

    private void RefreshStoryBank(string? search = null)
    {
        var rows = _store.ListStoryBank(search);
        StoryBankGrid.ItemsSource = rows;
        if (_story is not null) StoryBankGrid.SelectedItem = rows.FirstOrDefault(x => x.Id == _story.Id);
        AssignmentStoryBox.ItemsSource = _store.ListStoryBank();
    }

    private void RefreshAssignments()
    {
        IEnumerable<NrcsAssignment> query = _store.ListAssignments();
        var mode = Convert.ToString(AssignmentFilterModeBox.SelectedItem) ?? "ALL";
        var day = (AssignmentDateFilter.SelectedDate ?? DateTime.Today).Date;
        if (mode == "DAY") query = query.Where(x => x.DueUtc.ToLocalTime().Date == day);
        else if (mode == "NEXT 7 DAYS")
        {
            var start = DateTime.Today;
            var end = start.AddDays(7);
            query = query.Where(x => x.DueUtc.ToLocalTime() >= start && x.DueUtc.ToLocalTime() < end);
        }
        else if (mode == "OVERDUE") query = query.Where(x => x.DueUtc.ToLocalTime() < DateTime.Now && !IsClosedAssignment(x.Status));
        else if (mode == "OPEN ONLY") query = query.Where(x => !IsClosedAssignment(x.Status));
        var rows = query.OrderBy(x => x.DueUtc).ThenBy(x => x.Priority).ToList();
        AssignmentGrid.ItemsSource = rows;
        AssignmentCountText.Text = $"{rows.Count} shown · {mode}";
        if (_assignment is not null) AssignmentGrid.SelectedItem = rows.FirstOrDefault(x => x.Id == _assignment.Id);
    }

    private static bool IsClosedAssignment(string status) => status is "DONE" or "CANCELLED";

    private void AssignmentFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || _loading) return;
        RefreshAssignments();
    }

    private void AssignmentToday_Click(object sender, RoutedEventArgs e)
    {
        AssignmentDateFilter.SelectedDate = DateTime.Today;
        AssignmentFilterModeBox.SelectedItem = "DAY";
        RefreshAssignments();
    }

    private void RefreshRundownStories()
    {
        if (_rundown is null)
        {
            RundownGrid.ItemsSource = null;
            RundownHeader.Text = "SELECT A RUNDOWN";
            RundownProgramText.Text = "NONE";
            RundownTimingText.Text = "";
            RundownOpenCloseInfoText.Text = "Program Opening & Closing: None";
            LiveStateText.Text = "OFF AIR";
            return;
        }

        var rows = _store.ListStories(_rundown.Id);
        RundownGrid.ItemsSource = rows;
        if (_rundownStory is not null) RundownGrid.SelectedItem = rows.FirstOrDefault(x => x.RundownItemId == _rundownStory.RundownItemId);
        var summary = _store.GetRundownSummary(_rundown.Id);
        RundownHeader.Text = $"{_rundown.Name} · {_rundown.ChannelId} · {_rundown.Status}";

        var dateSys = _store.GetSetting("DateSystem", "BS");
        var num = _store.GetSetting("NumeralSystem", "Devanagari");
        var dev = !num.Equals("Standard", StringComparison.OrdinalIgnoreCase);
        var localAir = _rundown.AirDateUtc.ToLocalTime();
        var dateFormatted = NepaliCalendarService.FormatDisplayDateTime(localAir, dateSys, dev);

        RundownProgramText.Text = string.IsNullOrEmpty(_rundown.ProgramName) ? "CUSTOM" : _rundown.ProgramName.ToUpperInvariant();
        RundownTimingText.Text = $"Air: {dateFormatted} · {summary.Stories} items · {summary.Skipped} skipped · total {TimeSpan.FromSeconds(summary.DurationSeconds):hh\\:mm\\:ss}";
        RundownOpenCloseInfoText.Text = $"🎬 OPENING CG: {(string.IsNullOrEmpty(_rundown.OpeningCg) ? "Default" : _rundown.OpeningCg)} | CLOSING CG: {(string.IsNullOrEmpty(_rundown.ClosingCg) ? "Default" : _rundown.ClosingCg)} | MEDIA: {(string.IsNullOrEmpty(_rundown.OpeningMedia) ? "None" : Path.GetFileName(_rundown.OpeningMedia))}";

        if (_cgCatalog.Count > 0)
        {
            RundownOpenCgBox.ItemsSource = _cgCatalog.Select(x => x.Name).ToArray();
            RundownCloseCgBox.ItemsSource = _cgCatalog.Select(x => x.Name).ToArray();
            RundownOpenCgBox.Text = _rundown.OpeningCg;
            RundownCloseCgBox.Text = _rundown.ClosingCg;
        }

        RefreshLiveOnly();
    }

    private void RefreshLiveOnly()
    {
        if (_loading || _rundown is null) return;
        var live = _store.GetLiveState(_rundown.Id);
        if (live is null || !live.OnAir)
        {
            LiveStateText.Text = "OFF AIR · no live story selected";
            OnAirLed.Fill = System.Windows.Media.Brushes.DimGray;
            OnAirText.Text = "OFF AIR";
            OnAirText.Foreground = System.Windows.Media.Brushes.Gray;
            return;
        }
        var story = _store.GetStory(live.StoryId);
        LiveStateText.Text = story is null ? "ON AIR · unknown story" : $"ON AIR · {story.Slug} · {story.Presenter} · {live.UpdatedUtc.ToLocalTime():HH:mm:ss}";
        OnAirLed.Fill = System.Windows.Media.Brushes.Red;
        OnAirText.Text = "ON AIR";
        OnAirText.Foreground = System.Windows.Media.Brushes.Salmon;
    }

    private void NewAssignment_Click(object sender, RoutedEventArgs e)
    {
        _assignment = _store.CreateAssignment();
        RefreshAssignments();
        AssignmentGrid.SelectedItem = _assignment;
        WorkspaceTabs.SelectedIndex = 2;
        StatusText.Text = "New assignment created.";
    }

    private void NewStory_Click(object sender, RoutedEventArgs e)
    {
        _story = _store.CreateStoryDraft();
        RefreshStoryBank();
        StoryBankGrid.SelectedItem = _story;
        WorkspaceTabs.SelectedIndex = 4;
        StatusText.Text = "Independent story created in Story Bank.";
    }

    private void NewRundown_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CreateRundownDialog(_store) { Owner = this };
        if (dlg.ShowDialog() == true && dlg.CreatedRundown != null)
        {
            _rundown = dlg.CreatedRundown;
            RefreshRundowns();
            RundownList.SelectedItem = _store.ListRundowns().FirstOrDefault(x => x.Id == _rundown.Id);
            WorkspaceTabs.SelectedIndex = 5;
            StatusText.Text = $"Created rundown {_rundown.Name} with program structure.";
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshAll();
        RefreshDiary();
        RefreshWires();
        RefreshConnectorHealth();
        StatusText.Text = "Refreshed from newsroom database.";
    }

    private void LoadProfessionalDemo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _rundown = _store.RebuildProfessionalDemo();
            RefreshAll();
            RundownList.SelectedItem = _store.ListRundowns().FirstOrDefault(x => x.Id == _rundown.Id);
            WorkspaceTabs.SelectedIndex = 5;
            StatusText.Text = "Professional demo loaded: Anchor / PKG / VO / SOT / LIVE / CG with MOS, Playout and Prompter data.";
        }
        catch (Exception ex) { StatusText.Text = "Demo load failed: " + ex.GetBaseException().Message; }
    }

    private void OpenPrompter_Click(object sender, RoutedEventArgs e)
    {
        if (StandaloneAppLauncher.Launch("Prompter", out var error)) StatusText.Text = "Prompter opened as a separate application.";
        else StatusText.Text = error;
    }

    private void DashboardRundownGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DashboardRundownGrid.SelectedItem is NrcsRundown r)
        {
            _rundown = r;
            RundownList.SelectedItem = r;
            RefreshRundownStories();
        }
    }

    // --- PLANNING & DIARY ---
    private void RefreshDiary(string? search = null)
    {
        var list = _diaryService.ListEvents(search);
        DiaryGrid.ItemsSource = list;
        if (_selectedEvent is not null) DiaryGrid.SelectedItem = list.FirstOrDefault(x => x.Id == _selectedEvent.Id);
    }

    private void SearchDiary_Click(object sender, RoutedEventArgs e) => RefreshDiary(DiarySearchBox.Text);
    private void DiarySearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RefreshDiary(DiarySearchBox.Text); }

    private void DiaryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedEvent = DiaryGrid.SelectedItem as PlanningEvent;
        if (_selectedEvent is null) return;
        DiaryTitleBox.Text = _selectedEvent.Title;
        DiaryCategoryBox.Text = _selectedEvent.Category;
        DiaryLocationBox.Text = _selectedEvent.Location;
        DiaryContactsBox.Text = _selectedEvent.Contacts;
        DiaryDescBox.Text = _selectedEvent.Description;
    }

    private void NewDiaryEvent_Click(object sender, RoutedEventArgs e)
    {
        _diaryService.AddEvent("NEW DIARY EVENT", "NEWS", "CITY CENTER", DateTime.UtcNow.AddDays(1), "Media Desk", "Newsroom event outline");
        RefreshDiary();
        StatusText.Text = "New event added to Planning Diary.";
    }

    private void ConvertDiaryToAssignment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEvent is null)
        {
            StatusText.Text = "Select a diary event first.";
            return;
        }

        var assignment = _store.CreateAssignment() with
        {
            Title = _selectedEvent.Title,
            Desk = _selectedEvent.Category,
            Location = _selectedEvent.Location,
            Angle = _selectedEvent.Description,
            Notes = $"Contacts: {_selectedEvent.Contacts}",
            DueUtc = _selectedEvent.EventDateUtc
        };
        _store.SaveAssignment(assignment);
        _assignment = assignment;
        RefreshAssignments();
        WorkspaceTabs.SelectedIndex = 2;
        StatusText.Text = $"Converted diary event '{_selectedEvent.Title}' into reporter assignment.";
    }

    // --- WIRES / INGEST ---
    private async void RefreshWires(string? query = null)
    {
        var articles = await _wireService.FetchWiresAsync(query);
        WireGrid.ItemsSource = articles;
        if (_selectedWire is not null) WireGrid.SelectedItem = articles.FirstOrDefault(x => x.Id == _selectedWire.Id);
    }

    private void SearchWires_Click(object sender, RoutedEventArgs e) => RefreshWires(WireSearchBox.Text);
    private void WireSearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RefreshWires(WireSearchBox.Text); }
    private void RefreshWires_Click(object sender, RoutedEventArgs e) => RefreshWires();

    private void WireGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedWire = WireGrid.SelectedItem as WireArticle;
        if (_selectedWire is null) return;
        WireArticleHeadline.Text = _selectedWire.Headline;
        WireArticleMeta.Text = $"{_selectedWire.Agency} · {_selectedWire.PublishedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} · Category: {_selectedWire.Category} · Urgency: {_selectedWire.Urgency}";
        WireArticleBody.Text = _selectedWire.Body;
    }

    private void ConvertWireToStory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWire is null)
        {
            StatusText.Text = "Select a wire article first.";
            return;
        }

        var story = _store.CreateStoryDraft(_selectedWire.Headline) with
        {
            Body = _selectedWire.Body,
            Category = _selectedWire.Category,
            Language = _selectedWire.Language,
            Priority = _selectedWire.Urgency == "URGENT" ? "URGENT" : "NORMAL",
            Notes = $"Ingested from wire service: {_selectedWire.Agency} ({_selectedWire.Id})"
        };
        _store.SaveStory(story);
        _story = story;
        RefreshStoryBank();
        WorkspaceTabs.SelectedIndex = 4;
        StatusText.Text = $"Wire converted into Story Bank draft: '{story.Slug}'";
    }

    // --- ASSIGNMENT DESK ---
    private void AssignmentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _assignment = AssignmentGrid.SelectedItem as NrcsAssignment;
        if (_assignment is null) return;
        AssignmentTitleBox.Text = _assignment.Title;
        AssignmentAngleBox.Text = _assignment.Angle;
        AssignmentAssigneeBox.Text = _assignment.Assignee;
        AssignmentDeskBox.SelectedItem = _assignment.Desk;
        AssignmentLocationBox.Text = _assignment.Location;
        AssignmentStatusBox.SelectedItem = _assignment.Status;
        AssignmentPriorityBox.SelectedItem = _assignment.Priority;
        AssignmentDueBox.Text = _assignment.DueUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        AssignmentNotesBox.Text = _assignment.Notes;
        AssignmentStoryBox.SelectedValue = _assignment.LinkedStoryId;
    }

    private void SaveAssignment_Click(object sender, RoutedEventArgs e)
    {
        if (_assignment is null) return;
        var due = DateTime.TryParse(AssignmentDueBox.Text, out var d) ? d.ToUniversalTime() : _assignment.DueUtc;
        _assignment = _assignment with
        {
            Title = AssignmentTitleBox.Text.Trim(), Angle = AssignmentAngleBox.Text.Trim(), Assignee = AssignmentAssigneeBox.Text.Trim(),
            Desk = Convert.ToString(AssignmentDeskBox.SelectedItem) ?? "NEWS", Location = AssignmentLocationBox.Text.Trim(),
            Status = Convert.ToString(AssignmentStatusBox.SelectedItem) ?? "PLANNED", Priority = Convert.ToString(AssignmentPriorityBox.SelectedItem) ?? "NORMAL",
            DueUtc = due, LinkedStoryId = Convert.ToString(AssignmentStoryBox.SelectedValue) ?? "", Notes = AssignmentNotesBox.Text.Trim(),
            UpdatedUtc = DateTime.UtcNow
        };
        _store.SaveAssignment(_assignment);
        RefreshAssignments();
        StatusText.Text = "Assignment saved.";
    }

    private void CreateStoryFromAssignment_Click(object sender, RoutedEventArgs e)
    {
        if (_assignment is null) return;
        var story = _store.CreateStoryFromAssignment(_assignment);
        _story = story;
        RefreshStoryBank();
        RefreshAssignments();
        StoryBankGrid.SelectedItem = story;
        WorkspaceTabs.SelectedIndex = 4;
        StatusText.Text = $"Created story '{story.Slug}' from assignment.";
    }

    private void DeleteAssignment_Click(object sender, RoutedEventArgs e)
    {
        if (_assignment is null) return;
        _store.DeleteAssignment(_assignment.Id);
        _assignment = null;
        RefreshAssignments();
        StatusText.Text = "Assignment deleted.";
    }

    // --- STORY BANK & SCRIPT EDITOR ---
    private void SearchStories_Click(object sender, RoutedEventArgs e) => RefreshStoryBank(StorySearchBox.Text);
    private void StorySearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) RefreshStoryBank(StorySearchBox.Text); }

    private void StoryBankGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _story = StoryBankGrid.SelectedItem as NrcsStory;
        if (_story is null) return;
        LoadStoryIntoEditor(_story);
    }

    private void InitializeFontFamilies()
    {
        _suppressFormatEvents = true;
        try
        {
            _allFontItems.Clear();
            foreach (var f in Fonts.SystemFontFamilies.OrderBy(x => x.Source))
            {
                _allFontItems.Add(new FontItem { Name = f.Source, Family = f });
            }
            if (EditorFontFamilyBox != null)
            {
                EditorFontFamilyBox.ItemsSource = _allFontItems;
                var preferred = _allFontItems.FirstOrDefault(f => f.Name.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase))
                             ?? _allFontItems.FirstOrDefault();
                if (preferred != null) EditorFontFamilyBox.SelectedItem = preferred;
            }
        }
        catch { }
        finally
        {
            _suppressFormatEvents = false;
        }
    }

    private void EditorFontSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFormatEvents || EditorFontFamilyBox == null) return;
        var q = EditorFontSearchBox.Text?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(q)
            ? _allFontItems
            : _allFontItems.Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

        _suppressFormatEvents = true;
        try
        {
            EditorFontFamilyBox.ItemsSource = filtered;
            if (filtered.Count > 0)
            {
                EditorFontFamilyBox.SelectedItem = filtered[0];
                EditorFontFamilyBox.IsDropDownOpen = true;
            }
        }
        finally { _suppressFormatEvents = false; }
    }

    public string GetStoryPlainText()
    {
        if (BodyRichBox == null) return string.Empty;
        var range = new TextRange(BodyRichBox.Document.ContentStart, BodyRichBox.Document.ContentEnd);
        return range.Text.TrimEnd('\r', '\n');
    }

    public string GetStoryRichXaml()
    {
        if (BodyRichBox == null) return string.Empty;
        try
        {
            var range = new TextRange(BodyRichBox.Document.ContentStart, BodyRichBox.Document.ContentEnd);
            using var ms = new MemoryStream();
            range.Save(ms, DataFormats.Xaml);
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    public void SetStoryContent(string? plainText, string? richXaml = null)
    {
        if (BodyRichBox == null) return;
        _suppressFormatEvents = true;
        try
        {
            BodyRichBox.Document.Blocks.Clear();
            if (!string.IsNullOrWhiteSpace(richXaml))
            {
                try
                {
                    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(richXaml));
                    var range = new TextRange(BodyRichBox.Document.ContentStart, BodyRichBox.Document.ContentEnd);
                    range.Load(ms, DataFormats.Xaml);
                    return;
                }
                catch { }
            }

        var text = plainText ?? string.Empty;
        var paragraphs = text.Replace("\r\n", "\n").Split(new[] { "\n\n" }, StringSplitOptions.None);
        foreach (var pText in paragraphs)
        {
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 6) };
            var lines = pText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) para.Inlines.Add(new LineBreak());
                var line = lines[i];
                if (line.StartsWith('[') && line.Contains(']'))
                {
                    var closeIdx = line.IndexOf(']');
                    var cue = line.Substring(0, closeIdx + 1);
                    var rest = line.Substring(closeIdx + 1);
                    var cueColor = GetCueColor(cue);
                    para.Inlines.Add(new Run(cue)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(cueColor),
                        Background = new SolidColorBrush(Color.FromArgb(50, cueColor.R, cueColor.G, cueColor.B))
                    });
                    if (!string.IsNullOrEmpty(rest)) para.Inlines.Add(new Run(rest));
                }
                else
                {
                    para.Inlines.Add(new Run(line));
                }
            }
            BodyRichBox.Document.Blocks.Add(para);
        }
            if (BodyRichBox.Document.Blocks.Count == 0)
            {
                BodyRichBox.Document.Blocks.Add(new Paragraph(new Run("")));
            }
        }
        finally
        {
            _suppressFormatEvents = false;
        }
    }

    private static Color GetCueColor(string cue)
    {
        var u = cue.ToUpperInvariant();
        if (u.Contains("ANCHOR") || u.Contains("OC")) return Color.FromRgb(147, 197, 253);
        if (u.Contains("VO")) return Color.FromRgb(134, 239, 172);
        if (u.Contains("PKG")) return Color.FromRgb(216, 180, 254);
        if (u.Contains("SOT")) return Color.FromRgb(253, 230, 138);
        if (u.Contains("LIVE")) return Color.FromRgb(252, 165, 165);
        if (u.Contains("CG")) return Color.FromRgb(153, 246, 228);
        if (u.Contains("DIRECTOR")) return Color.FromRgb(251, 207, 232);
        if (u.Contains("PRODUCER")) return Color.FromRgb(233, 213, 255);
        return Color.FromRgb(229, 231, 235);
    }

    private void LoadStoryIntoEditor(NrcsStory s)
    {
        SlugBox.Text = s.Slug;
        PresenterBox.Text = s.Presenter;
        WriterBox.Text = s.Writer;
        EditorBox.Text = s.Editor;
        StoryStatusBox.SelectedItem = s.Status;
        StoryPriorityBox.SelectedItem = s.Priority;
        StoryCategoryBox.SelectedItem = s.Category;
        StoryTypeBox.SelectedItem = s.StoryType;
        DurationBox.Text = s.DurationSeconds.ToString();
        LanguageBox.Text = s.Language;
        SetStoryContent(s.Body, s.BodyRichXaml);
        StoryNotesBox.Text = s.Notes;
        MediaPathBox.Text = s.MediaPath;
        CgTemplateBox.Text = s.CgTemplate;
        CgLayerBox.Text = s.CgLayer.ToString();
        CgDataBox.Text = s.CgDataJson;
        UpdateCgTemplateInfo(s.CgTemplate);
        UpdateScriptTiming(s.Body);
        var versions = _store.ListStoryVersions(s.Id);
        VersionGrid.ItemsSource = versions;
        _selectedVersion = versions.FirstOrDefault();
        UpdateVersionCompare();
    }

    private void EditorFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _suppressFormatEvents || BodyRichBox == null) return;
        FontFamily? family = null;
        if (EditorFontFamilyBox?.SelectedItem is FontItem fi)
            family = fi.Family;
        else if (EditorFontFamilyBox?.SelectedItem is FontFamily ff)
            family = ff;
        else if (!string.IsNullOrWhiteSpace(EditorFontFamilyBox?.Text))
            family = new FontFamily(EditorFontFamilyBox.Text.Trim());

        if (family is not null)
        {
            BodyRichBox.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, family);
            BodyRichBox.Focus();
        }
    }

    private void EditorFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _suppressFormatEvents || BodyRichBox == null) return;
        var text = (EditorFontSizeBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? EditorFontSizeBox?.Text;
        if (double.TryParse(text, out var size))
        {
            size = Math.Clamp(size, 8, 120);
            BodyRichBox.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            BodyRichBox.Focus();
        }
    }

    private void ToggleBold_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null) return;
        var sel = BodyRichBox.Selection;
        var cur = sel.GetPropertyValue(TextElement.FontWeightProperty);
        var next = (cur is FontWeight w && w == FontWeights.Bold) ? FontWeights.Normal : FontWeights.Bold;
        sel.ApplyPropertyValue(TextElement.FontWeightProperty, next);
        UpdateFormattingButtonStates();
        BodyRichBox.Focus();
    }

    private void ToggleItalic_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null) return;
        var sel = BodyRichBox.Selection;
        var cur = sel.GetPropertyValue(TextElement.FontStyleProperty);
        var next = (cur is FontStyle s && s == FontStyles.Italic) ? FontStyles.Normal : FontStyles.Italic;
        sel.ApplyPropertyValue(TextElement.FontStyleProperty, next);
        UpdateFormattingButtonStates();
        BodyRichBox.Focus();
    }

    private void ToggleUnderline_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null) return;
        var sel = BodyRichBox.Selection;
        var cur = sel.GetPropertyValue(Inline.TextDecorationsProperty);
        var isUnderlined = cur == TextDecorations.Underline;
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty, isUnderlined ? null : TextDecorations.Underline);
        BodyRichBox.Focus();
    }

    private void ToggleStrike_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null) return;
        var sel = BodyRichBox.Selection;
        var cur = sel.GetPropertyValue(Inline.TextDecorationsProperty);
        var isStrike = cur == TextDecorations.Strikethrough;
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty, isStrike ? null : TextDecorations.Strikethrough);
        BodyRichBox.Focus();
    }

    private void ResetFormatting_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null) return;
        var sel = BodyRichBox.Selection;
        sel.ClearAllProperties();
        sel.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Segoe UI, Mangal, Nirmala UI"));
        sel.ApplyPropertyValue(TextElement.FontSizeProperty, 15.0);
        sel.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
        sel.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
        sel.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Color.FromRgb(231, 234, 237)));
        sel.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Transparent);
        sel.ApplyPropertyValue(Inline.TextDecorationsProperty, null);
        UpdateFormattingButtonStates();
        BodyRichBox.Focus();
    }

    private void EditorTextColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _suppressFormatEvents || BodyRichBox == null || EditorTextColorBox?.SelectedItem is not ComboBoxItem item) return;
        var col = Convert.ToString(item.Content) switch
        {
            "Yellow" => Color.FromRgb(254, 240, 138),
            "Cyan" => Color.FromRgb(103, 232, 249),
            "Green" => Color.FromRgb(134, 239, 172),
            "Orange" => Color.FromRgb(253, 186, 116),
            "Pink" => Color.FromRgb(244, 114, 182),
            "Light Blue" => Color.FromRgb(147, 197, 253),
            "Red" => Color.FromRgb(252, 165, 165),
            _ => Color.FromRgb(231, 234, 237)
        };
        BodyRichBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(col));
        BodyRichBox.Focus();
    }

    private void EditorBgColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _suppressFormatEvents || BodyRichBox == null || EditorBgColorBox?.SelectedItem is not ComboBoxItem item) return;
        var brush = Convert.ToString(item.Content) switch
        {
            "Yellow" => new SolidColorBrush(Color.FromArgb(160, 202, 138, 4)),
            "Cyan" => new SolidColorBrush(Color.FromArgb(160, 8, 145, 178)),
            "Dark Blue" => new SolidColorBrush(Color.FromArgb(180, 30, 58, 138)),
            "Dark Red" => new SolidColorBrush(Color.FromArgb(180, 127, 29, 29)),
            "Dark Green" => new SolidColorBrush(Color.FromArgb(180, 20, 83, 45)),
            "Charcoal" => new SolidColorBrush(Color.FromArgb(200, 39, 39, 42)),
            _ => Brushes.Transparent
        };
        BodyRichBox.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
        BodyRichBox.Focus();
    }

    private void BodyRichBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateFormattingButtonStates();
    }

    private void UpdateFormattingButtonStates()
    {
        if (BodyRichBox == null || _loading) return;
        var sel = BodyRichBox.Selection;

        _suppressFormatEvents = true;
        try
        {
            if (EditorFontFamilyBox != null)
            {
                var curFont = sel.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily;
                if (curFont != null && EditorFontFamilyBox.ItemsSource is IEnumerable<FontFamily> fontList)
                {
                    var match = fontList.FirstOrDefault(f => f.Source.Equals(curFont.Source, StringComparison.OrdinalIgnoreCase));
                    if (match != null && !Equals(EditorFontFamilyBox.SelectedItem, match))
                    {
                        EditorFontFamilyBox.SelectedItem = match;
                    }
                }
            }

            if (EditorFontSizeBox != null)
            {
                var curSize = sel.GetPropertyValue(TextElement.FontSizeProperty);
                if (curSize is double dSize)
                {
                    var rounded = (int)Math.Round(dSize);
                    foreach (ComboBoxItem item in EditorFontSizeBox.Items)
                    {
                        if (int.TryParse(Convert.ToString(item.Content), out var sz) && sz == rounded)
                        {
                            if (!Equals(EditorFontSizeBox.SelectedItem, item))
                                EditorFontSizeBox.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }
        catch { }
        finally
        {
            _suppressFormatEvents = false;
        }

        var isBold = sel.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight w && w == FontWeights.Bold;
        if (BoldBtn != null)
        {
            BoldBtn.Background = isBold
                ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                : new SolidColorBrush(Color.FromRgb(30, 41, 59));
        }
        var isItalic = sel.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle s && s == FontStyles.Italic;
        if (ItalicBtn != null)
        {
            ItalicBtn.Background = isItalic
                ? new SolidColorBrush(Color.FromRgb(59, 130, 246))
                : new SolidColorBrush(Color.FromRgb(30, 41, 59));
        }
    }

    private void ReadingRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ReadingRateValueText != null)
        {
            ReadingRateValueText.Text = $"{(int)Math.Round(e.NewValue)} WPM";
        }
        UpdateScriptTiming(GetStoryPlainText());
    }

    private void BodyRichBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateScriptTiming(GetStoryPlainText());
    }

    private void UpdateScriptTiming(string? text)
    {
        if (ScriptTimingStatsText == null) return;
        var t = text ?? string.Empty;
        var words = t.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var wpm = (ReadingRateSlider != null && ReadingRateSlider.Value >= 60) ? ReadingRateSlider.Value : 140.0;
        var wordsPerSec = Math.Max(1.0, wpm / 60.0);
        var estimatedSec = (int)Math.Ceiling(words / wordsPerSec);
        ScriptTimingStatsText.Text = $"Words: {words} · Est. Read Time: {estimatedSec}s (at {(int)Math.Round(wpm)} WPM)";

        if (AutoSyncDurationCheck?.IsChecked == true && DurationBox != null && words > 0)
        {
            DurationBox.Text = Math.Max(5, estimatedSec).ToString();
        }
    }

    private void InsertCue_Click(object sender, RoutedEventArgs e)
    {
        if (BodyRichBox == null || sender is not Button b || b.Tag is not string cue) return;
        var cueColor = GetCueColor(cue);
        var tr = BodyRichBox.Selection;
        tr.Text = cue + " ";
        tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
        tr.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(cueColor));
        tr.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(55, cueColor.R, cueColor.G, cueColor.B)));
        BodyRichBox.CaretPosition = tr.End;
        BodyRichBox.Focus();
    }

    private async void AiAssist_Click(object sender, RoutedEventArgs e)
    {
        var text = GetStoryPlainText();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Please write or paste script text into the body editor first.";
            return;
        }

        try
        {
            var ai = _connectorManager.Ai;
            var headline = await ai.GenerateHeadlineAsync(text);
            var summary = await ai.GenerateSummaryAsync(text);
            var tags = await ai.GenerateTagsAsync(text);

            var dialog = System.Windows.MessageBox.Show(
                $"AI SUGGESTIONS:\n\nHEADLINE:\n{headline}\n\nSUMMARY / BULLET:\n{summary}\n\nTAGS:\n{tags}\n\nApply suggested headline and add summary to private story notes?",
                "AI Editorial Assistant",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);

            if (dialog == System.Windows.MessageBoxResult.Yes)
            {
                SlugBox.Text = headline;
                StoryNotesBox.Text = (StoryNotesBox.Text + "\n[AI Summary]: " + summary + "\n[AI Tags]: " + tags).Trim();
                StatusText.Text = "AI suggestions approved and applied to story.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "AI Assistance error: " + ex.GetBaseException().Message;
        }
    }

    private void VersionGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedVersion = VersionGrid.SelectedItem as NrcsStoryVersion;
        UpdateVersionCompare();
    }

    private void UpdateVersionCompare()
    {
        CurrentRevisionBodyBox.Text = GetStoryPlainText();
        SelectedRevisionBodyBox.Text = _selectedVersion?.Body ?? "(No revision selected)";
        VersionCompareText.Text = _selectedVersion is null ? "Select revision to compare" : $"Viewing revision {_selectedVersion.VersionId} ({_selectedVersion.CreatedUtc.ToLocalTime():HH:mm:ss})";
    }

    private void RestoreRevision_Click(object sender, RoutedEventArgs e)
    {
        if (_story is null || _selectedVersion is null) { StatusText.Text = "Select a revision first."; return; }
        SetStoryContent(_selectedVersion.Body, null);
        SlugBox.Text = _selectedVersion.Slug;
        StatusText.Text = $"Restored script from revision {_selectedVersion.VersionId}.";
    }

    private NrcsStory? StoryFromEditor()
    {
        if (_story is null) return null;
        var duration = int.TryParse(DurationBox.Text, out var d) ? Math.Max(1, d) : 30;
        var layer = int.TryParse(CgLayerBox.Text, out var l) ? Math.Clamp(l, 0, 999) : 20;
        var plain = GetStoryPlainText();
        var xaml = GetStoryRichXaml();
        return _story with
        {
            Slug = SlugBox.Text.Trim(), Presenter = PresenterBox.Text.Trim(), Writer = WriterBox.Text.Trim(), Editor = EditorBox.Text.Trim(),
            Status = Convert.ToString(StoryStatusBox.SelectedItem) ?? "DRAFT", Priority = Convert.ToString(StoryPriorityBox.SelectedItem) ?? "NORMAL",
            Category = Convert.ToString(StoryCategoryBox.SelectedItem) ?? "NEWS", StoryType = Convert.ToString(StoryTypeBox.SelectedItem) ?? "PKG", Language = string.IsNullOrWhiteSpace(LanguageBox.Text) ? "en" : LanguageBox.Text.Trim(),
            DurationSeconds = duration, Body = plain, BodyRichXaml = xaml, Notes = StoryNotesBox.Text, MediaPath = MediaPathBox.Text.Trim(),
            CgTemplate = CgTemplateBox.Text.Trim(), CgLayer = layer, CgDataJson = CgDataBox.Text, UpdatedUtc = DateTime.UtcNow
        };
    }

    private void SaveStory_Click(object sender, RoutedEventArgs e)
    {
        var row = StoryFromEditor(); if (row is null) return;
        _story = row; _store.SaveStory(row); RefreshStoryBank(StorySearchBox.Text); StoryBankGrid.SelectedItem = _story; VersionGrid.ItemsSource = _store.ListStoryVersions(row.Id);
        RefreshRundownStories(); StatusText.Text = "Story saved and revision captured.";
    }

    private void DeleteStory_Click(object sender, RoutedEventArgs e)
    {
        if (_story is null) return;
        _store.DeleteStory(_story.Id); _story = null; RefreshStoryBank(StorySearchBox.Text); RefreshAssignments(); RefreshRundownStories(); StatusText.Text = "Story deleted from Story Bank and rundowns.";
    }

    private void BrowseMedia_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Attach MAM / media file", Filter = "Media files|*.mxf;*.mov;*.mp4;*.mkv;*.avi;*.ts;*.m2ts;*.wav;*.mpg;*.mpeg|All files|*.*" };
        if (d.ShowDialog() == true) MediaPathBox.Text = d.FileName;
    }

    private void AddStoryToRundown_Click(object sender, RoutedEventArgs e)
    {
        var row = StoryFromEditor(); if (row is not null) { _story = row; _store.SaveStory(row); }
        if (_story is null) { StatusText.Text = "Select a Story Bank story first."; return; }
        if (_rundown is null) { StatusText.Text = "Select/create a rundown in Rundown / Live first."; return; }
        _store.AddStoryToRundown(_story.Id, _rundown.Id); RefreshRundownStories(); WorkspaceTabs.SelectedIndex = 5; StatusText.Text = $"Added {_story.Slug} to {_rundown.Name}.";
    }

    // --- RUNDOWN / LIVE PRODUCTION ---
    private void RundownList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _rundown = RundownList.SelectedItem as NrcsRundown;
        _rundownStory = null;
        if (_rundown != null)
        {
            RundownOpenCgBox.Text = _rundown.OpeningCg ?? "";
            RundownCloseCgBox.Text = _rundown.ClosingCg ?? "";
        }
        RefreshRundownStories();
    }

    private void RundownGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _rundownStory = RundownGrid.SelectedItem as NrcsStory;
        if (_rundownStory is null) return;
        RundownItemSlug.Text = $"{_rundownStory.Sequence:00} · {_rundownStory.Slug} · {_rundownStory.Presenter}";
        RundownItemTitleBox.Text = _rundownStory.Slug;
        RundownItemDurBox.Text = _rundownStory.DurationSeconds.ToString();
        SegmentBox.Text = _rundownStory.Segment;
        StartModeBox.SelectedItem = _rundownStory.StartMode;
        ItemStatusBox.SelectedItem = _rundownStory.ItemStatus;
        SkipCheck.IsChecked = _rundownStory.IsSkipped;
        LockCheck.IsChecked = _rundownStory.IsLocked;
    }

    private void SaveRundownItem_Click(object sender, RoutedEventArgs e)
    {
        if (_rundownStory is null) return;
        int.TryParse(RundownItemDurBox.Text.Trim(), out var durSec);
        if (durSec <= 0) durSec = _rundownStory.DurationSeconds;
        var newSlug = string.IsNullOrWhiteSpace(RundownItemTitleBox.Text) ? _rundownStory.Slug : RundownItemTitleBox.Text.Trim();
        _rundownStory = _rundownStory with
        {
            Slug = newSlug,
            DurationSeconds = durSec,
            Segment = SegmentBox.Text.Trim(),
            StartMode = Convert.ToString(StartModeBox.SelectedItem) ?? "FOLLOW",
            ItemStatus = Convert.ToString(ItemStatusBox.SelectedItem) ?? "READY",
            IsSkipped = SkipCheck.IsChecked == true,
            IsLocked = LockCheck.IsChecked == true
        };
        _store.UpdateRundownItem(_rundownStory);
        RefreshRundownStories();
        StatusText.Text = "Rundown item updated.";
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) { if (_rundown is null || _rundownStory is null || _rundownStory.IsLocked) return; _store.MoveRundownItem(_rundown.Id, _rundownStory.RundownItemId, -1); RefreshRundownStories(); }
    private void MoveDown_Click(object sender, RoutedEventArgs e) { if (_rundown is null || _rundownStory is null || _rundownStory.IsLocked) return; _store.MoveRundownItem(_rundown.Id, _rundownStory.RundownItemId, 1); RefreshRundownStories(); }
    private void RemoveRundownItem_Click(object sender, RoutedEventArgs e) { if (_rundownStory is null || _rundownStory.IsLocked) return; _store.RemoveRundownItem(_rundownStory.RundownItemId); _rundownStory = null; RefreshRundownStories(); StatusText.Text = "Story reference removed from rundown; Story Bank copy retained."; }

    private async void TakeLive_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null || _rundownStory is null || _rundownStory.IsSkipped) return;
        _store.SetLiveStory(_rundown.Id, _rundownStory.Id, _rundownStory.RundownItemId, true); _store.SetRundownStatus(_rundown.Id, "ON AIR"); RefreshRundowns(); RefreshRundownStories(); StatusText.Text = $"Live story: {_rundownStory.Slug}";
        if (AutoAirCheck.IsChecked == true) await TriggerStoryAutomationAsync(_rundownStory);
    }

    private async void NextLive_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null) return;
        var rows = _store.ListStories(_rundown.Id).Where(x => !x.IsSkipped).ToList(); if (rows.Count == 0) return;
        var live = _store.GetLiveState(_rundown.Id); var index = live is null ? -1 : rows.FindIndex(x => x.RundownItemId == live.RundownItemId); var next = rows[Math.Min(rows.Count - 1, index + 1)];
        _store.SetLiveStory(_rundown.Id, next.Id, next.RundownItemId, true); _rundownStory = next; RefreshRundownStories(); StatusText.Text = $"Next live: {next.Slug}";
        if (AutoAirCheck.IsChecked == true) await TriggerStoryAutomationAsync(next);
    }

    private string ResolveLiveChannelId()
    {
        var configured = _rundown?.ChannelId;
        if (!string.IsNullOrWhiteSpace(configured) && PlatformControlBus.ReadStatuses().Any(x => string.Equals(x.ChannelId, configured, StringComparison.OrdinalIgnoreCase))) return configured;
        return PlatformControlBus.ReadStatuses().OrderByDescending(x => x.UpdatedUtc).FirstOrDefault()?.ChannelId ?? configured ?? "KTX-PLAYOUT-01";
    }

    private async Task TriggerStoryAutomationAsync(NrcsStory story)
    {
        try
        {
            var channelId = ResolveLiveChannelId();
            var response = await PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand
            {
                ChannelId = channelId, Action = "play_story",
                Parameters = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) { ["storyId"] = story.Id, ["slug"] = story.Slug }
            }, TimeSpan.FromSeconds(3));
            if (!response.Ok) { StatusText.Text = $"Live state set; playout rejected story: {response.Message}"; return; }
            if (!string.IsNullOrWhiteSpace(story.CgTemplate))
            {
                var projects = _database.LoadState("cg-projects", new List<CgProject>());
                var project = projects.FirstOrDefault(x => string.Equals(x.Name, story.CgTemplate, StringComparison.OrdinalIgnoreCase));
                if (project is not null)
                {
                    project.ExternalLayer = story.CgLayer;
                    LocalCgCommandBus.Publish(new[] { channelId }, new CgRemoteCommand { Action = "PLAY", Bus = "PROGRAM", Layer = story.CgLayer, Project = project });
                }
            }
            StatusText.Text = $"ON AIR automation sent: {story.Slug} → {channelId} · CG {story.CgTemplate} layer {story.CgLayer}";
        }
        catch (Exception ex) { StatusText.Text = "Live automation failed: " + ex.GetBaseException().Message; }
    }

    private void ClearLive_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null) return; _store.SetLiveStory(_rundown.Id, "", "", false); RefreshRundownStories(); StatusText.Text = "On-air story state cleared.";
    }

    private void PublishPlayout_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null) return;
        try { var count = _store.PublishToPlayout(_rundown.Id); RefreshRundowns(); StatusText.Text = $"Published {count} playout events to {_rundown.ChannelId}."; }
        catch (Exception ex) { StatusText.Text = "Publish failed: " + ex.GetBaseException().Message; }
    }

    private async void PublishMos_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null) return;
        try { var result = await _store.PublishMosAsync(_rundown.Id); StatusText.Text = result.Sent ? $"MOS sent · {result.Message} · {result.Path}" : $"MOS outbox saved; network send failed · {result.Message} · {result.Path}"; }
        catch (Exception ex) { StatusText.Text = "MOS publish failed: " + ex.GetBaseException().Message; }
    }

    // --- NEPALI CALENDAR INTEGRATION ---
    private void UpdateLiveCalendarDate()
    {
        try
        {
            var dateSys = _store.GetSetting("DateSystem", "BS");
            var devnagari = _store.GetSetting("DevnagariDigits", "False").Equals("True", StringComparison.OrdinalIgnoreCase);
            var now = DateTime.Now;
            var formatted = NepaliCalendarService.FormatDisplayDate(now, dateSys, devnagari);
            if (TopNepaliDateText != null)
            {
                TopNepaliDateText.Text = $"📅 {formatted}";
            }
        }
        catch { }
    }

    private void OpenNepaliCalendar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new NepaliCalendarDialog(_store) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            UpdateLiveCalendarDate();
            RefreshAll();
            StatusText.Text = $"Date preference updated to {dlg.DateSystem}. Selected: {dlg.SelectedFormattedDate}";
        }
    }

    // --- RUNDOWN STORY / BREAK / SEGMENT ACTIONS ---
    private void AddStoryToRundownManual_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Please select or create a rundown first.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var newStory = _store.CreateStoryDraft("NEW STORY BLOCK") with
        {
            Presenter = "ANCHOR",
            StoryType = "ANCHOR",
            Category = "NEWS",
            Priority = "NORMAL",
            Status = "DRAFT",
            DurationSeconds = 60,
            Segment = "A",
            StartMode = "FOLLOW",
            ItemStatus = "READY",
            Body = "Enter story script here..."
        };

        _store.SaveStory(newStory);
        _store.AddStoryToRundown(newStory.Id, _rundown.Id);
        RefreshRundownStories();
        StatusText.Text = $"Added new story to {_rundown.Name}.";
    }

    private void AddBreakToRundown_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Please select or create a rundown first.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new AddBreakDialog(_store, _rundown.ProgramId) { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _store.AddBreakToRundown(_rundown.Id, dlg.BreakTitle, dlg.DurationSeconds, dlg.Segment, dlg.BreakType);
            RefreshRundownStories();
            StatusText.Text = $"Inserted {dlg.BreakType}: {dlg.BreakTitle} ({dlg.DurationSeconds}s).";
        }
    }

    private void AddSegmentToRundown_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Please select or create a rundown first.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new AddBreakDialog(_store, _rundown.ProgramId, isSegmentHeader: true) { Owner = this };
        dlg.Title = "Add Segment Header / Topic Block";
        if (dlg.ShowDialog() == true)
        {
            _store.AddSegmentHeaderToRundown(_rundown.Id, dlg.BreakTitle, dlg.Segment);
            RefreshRundownStories();
            StatusText.Text = $"Inserted Segment Block: {dlg.BreakTitle} [Seg {dlg.Segment}].";
        }
    }

    private void SaveRundownTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Please select an active rundown first.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDlg = new SaveFileDialog
        {
            Title = "Save Rundown Template",
            Filter = "Kashtrix Rundown Template (*.ktxtpl)|*.ktxtpl|JSON File (*.json)|*.json",
            FileName = $"{_rundown.Name.Replace(" ", "_")}_Template.ktxtpl"
        };

        if (saveDlg.ShowDialog() == true)
        {
            try
            {
                var stories = _store.ListStories(_rundown.Id);
                var templateData = new
                {
                    TemplateVersion = "1.0",
                    RundownName = _rundown.Name,
                    ProgramId = _rundown.ProgramId,
                    ProgramName = _rundown.ProgramName,
                    OpeningMedia = _rundown.OpeningMedia,
                    ClosingMedia = _rundown.ClosingMedia,
                    OpeningCg = _rundown.OpeningCg,
                    ClosingCg = _rundown.ClosingCg,
                    Items = stories.Select(s => new
                    {
                        s.Slug,
                        s.StoryType,
                        s.Presenter,
                        s.Category,
                        s.DurationSeconds,
                        s.Segment,
                        s.StartMode,
                        s.Notes,
                        s.CgTemplate,
                        s.CgLayer
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(templateData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveDlg.FileName, json);
                StatusText.Text = $"Rundown template saved successfully: {Path.GetFileName(saveDlg.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to save template: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadRundownTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Please select an active rundown to populate from template.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var openDlg = new OpenFileDialog
        {
            Title = "Load Rundown Template",
            Filter = "Kashtrix Rundown Template (*.ktxtpl;*.json)|*.ktxtpl;*.json|All Files (*.*)|*.*"
        };

        if (openDlg.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(openDlg.FileName);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("Items", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                {
                    int added = 0;
                    foreach (var item in itemsElem.EnumerateArray())
                    {
                        var slug = item.TryGetProperty("Slug", out var sp) ? sp.GetString() ?? "ITEM" : "ITEM";
                        var type = item.TryGetProperty("StoryType", out var stp) ? stp.GetString() ?? "ANCHOR" : "ANCHOR";
                        var presenter = item.TryGetProperty("Presenter", out var prp) ? prp.GetString() ?? "" : "";
                        var category = item.TryGetProperty("Category", out var cp) ? cp.GetString() ?? "NEWS" : "NEWS";
                        var dur = item.TryGetProperty("DurationSeconds", out var dp) ? dp.GetInt32() : 30;
                        var seg = item.TryGetProperty("Segment", out var sgp) ? sgp.GetString() ?? "A" : "A";
                        var startMode = item.TryGetProperty("StartMode", out var smp) ? smp.GetString() ?? "FOLLOW" : "FOLLOW";
                        var notes = item.TryGetProperty("Notes", out var np) ? np.GetString() ?? "" : "";

                        var story = _store.CreateStoryDraft(slug) with
                        {
                            StoryType = type,
                            Presenter = presenter,
                            Category = category,
                            DurationSeconds = dur,
                            Segment = seg,
                            StartMode = startMode,
                            Notes = notes,
                            Status = "READY"
                        };
                        _store.SaveStory(story);
                        _store.AddStoryToRundown(story.Id, _rundown.Id);
                        added++;
                    }

                    RefreshRundownStories();
                    StatusText.Text = $"Loaded template: {added} items imported into {_rundown.Name}.";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to load template: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void DeleteRundown_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Select a rundown to delete.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete rundown '{_rundown.Name}'?\nStories in the Story Bank will NOT be deleted.", "Confirm Rundown Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            _store.DeleteRundown(_rundown.Id);
            _rundown = null;
            RefreshRundowns();
            RefreshRundownStories();
            StatusText.Text = "Rundown deleted.";
        }
    }

    private void ApplyRundownProgramSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_rundown is null)
        {
            System.Windows.MessageBox.Show("Select a rundown first.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var updated = _rundown with
        {
            OpeningCg = RundownOpenCgBox.Text.Trim(),
            ClosingCg = RundownCloseCgBox.Text.Trim()
        };

        _store.SaveRundown(updated);
        _rundown = updated;
        RefreshRundowns();
        StatusText.Text = $"Updated opening/closing CG templates for {_rundown.Name}.";
    }

    // --- PROGRAMS & PRESETS CRUD ---
    private void RefreshPrograms()
    {
        try
        {
            var progs = _store.ListPrograms();
            ProgramListBox.ItemsSource = progs;

            var cgNames = _cgCatalog.Select(x => x.Name).ToList();
            ProgOpenCgBox.ItemsSource = cgNames;
            ProgCloseCgBox.ItemsSource = cgNames;
            RundownOpenCgBox.ItemsSource = cgNames;
            RundownCloseCgBox.ItemsSource = cgNames;

            var cats = _store.ListCategories().Select(c => c.Code).ToList();
            ProgCategoryBox.ItemsSource = cats;

            var filterList = new List<string> { "ALL PROGRAMS", "GLOBAL PRESETS" };
            filterList.AddRange(progs.Select(p => p.Name));
            BreakFilterBox.ItemsSource = filterList;
            if (BreakFilterBox.SelectedIndex < 0) BreakFilterBox.SelectedIndex = 0;

            var targetList = new List<string> { "GLOBAL (All Programs)" };
            targetList.AddRange(progs.Select(p => $"{p.Name} [{p.Id}]"));
            BreakTargetProgBox.ItemsSource = targetList;
            if (BreakTargetProgBox.SelectedIndex < 0) BreakTargetProgBox.SelectedIndex = 0;
        }
        catch { }
    }

    private void ProgramList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProgramListBox.SelectedItem is not NrcsProgram p) return;
        _selectedProgram = p;
        ProgNameBox.Text = p.Name;
        ProgCategoryBox.SelectedItem = p.Category;
        ProgDurationBox.Text = p.DefaultDurationMinutes.ToString();
        ProgAnchorBox.Text = p.DefaultAnchor;
        ProgChannelBox.Text = p.ChannelId;
        ProgStudioBox.Text = p.Studio;
        ProgOpenMediaBox.Text = p.OpeningMedia;
        ProgOpenCgBox.Text = p.OpeningCg;
        ProgCloseMediaBox.Text = p.ClosingMedia;
        ProgCloseCgBox.Text = p.ClosingCg;
        ProgDescBox.Text = p.Description;
        ProgActiveCheck.IsChecked = p.IsActive;
    }

    private void NewProgram_Click(object sender, RoutedEventArgs e)
    {
        _selectedProgram = null;
        ProgramListBox.SelectedIndex = -1;
        ProgNameBox.Text = "NEW BROADCAST SHOW";
        ProgCategoryBox.SelectedItem = "NEWS";
        ProgDurationBox.Text = "30";
        ProgAnchorBox.Text = "";
        ProgChannelBox.Text = "KTX-PLAYOUT-01";
        ProgStudioBox.Text = "STUDIO-A";
        ProgOpenMediaBox.Text = "";
        ProgOpenCgBox.Text = "";
        ProgCloseMediaBox.Text = "";
        ProgCloseCgBox.Text = "";
        ProgDescBox.Text = "";
        ProgActiveCheck.IsChecked = true;
        ProgNameBox.Focus();
        ProgNameBox.SelectAll();
    }

    private void SaveProgram_Click(object sender, RoutedEventArgs e)
    {
        var name = ProgNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show("Please enter a program name.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(ProgDurationBox.Text.Trim(), out var dur);
        if (dur <= 0) dur = 30;

        var prog = new NrcsProgram(
            _selectedProgram?.Id ?? ("PROG-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()),
            name,
            Convert.ToString(ProgCategoryBox.SelectedItem) ?? "NEWS",
            dur,
            ProgAnchorBox.Text.Trim(),
            ProgChannelBox.Text.Trim(),
            ProgStudioBox.Text.Trim(),
            ProgOpenMediaBox.Text.Trim(),
            ProgCloseMediaBox.Text.Trim(),
            ProgOpenCgBox.Text.Trim(),
            ProgCloseCgBox.Text.Trim(),
            ProgDescBox.Text.Trim(),
            ProgActiveCheck.IsChecked == true,
            DateTime.UtcNow
        );

        _store.SaveProgram(prog);
        _selectedProgram = prog;
        RefreshPrograms();
        StatusText.Text = $"Program '{prog.Name}' saved successfully.";
    }

    private void DeleteProgram_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProgram is null)
        {
            System.Windows.MessageBox.Show("Select a program to delete.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete program '{_selectedProgram.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _store.DeleteProgram(_selectedProgram.Id);
            _selectedProgram = null;
            RefreshPrograms();
            NewProgram_Click(sender, e);
            StatusText.Text = "Program deleted.";
        }
    }

    private void BrowseProgOpenMedia_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Select Opening Media Master", Filter = "Video Files|*.mp4;*.mov;*.mxf;*.mkv;*.ts;*.avi|All Files|*.*" };
        if (d.ShowDialog() == true) ProgOpenMediaBox.Text = d.FileName;
    }

    private void BrowseProgCloseMedia_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Select Closing Media Master", Filter = "Video Files|*.mp4;*.mov;*.mxf;*.mkv;*.ts;*.avi|All Files|*.*" };
        if (d.ShowDialog() == true) ProgCloseMediaBox.Text = d.FileName;
    }

    // --- BREAK PRESETS CRUD ---
    private void RefreshBreakPresets()
    {
        try
        {
            var filter = BreakFilterBox.SelectedItem as string;
            IReadOnlyList<NrcsBreakTemplate> presets;
            if (string.Equals(filter, "GLOBAL PRESETS", StringComparison.OrdinalIgnoreCase))
            {
                presets = _store.ListBreakTemplates(null).Where(b => string.IsNullOrEmpty(b.ProgramId)).ToList();
            }
            else if (!string.IsNullOrEmpty(filter) && !string.Equals(filter, "ALL PROGRAMS", StringComparison.OrdinalIgnoreCase))
            {
                var prog = _store.ListPrograms().FirstOrDefault(p => p.Name.Equals(filter, StringComparison.OrdinalIgnoreCase));
                presets = prog != null ? _store.ListBreakTemplates(prog.Id) : _store.ListBreakTemplates(null);
            }
            else
            {
                presets = _store.ListBreakTemplates(null);
            }

            BreakPresetGrid.ItemsSource = presets;
        }
        catch { }
    }

    private void BreakFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshBreakPresets();
    }

    private void BreakPresetGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BreakPresetGrid.SelectedItem is not NrcsBreakTemplate b) return;
        _selectedBreakPreset = b;
        BreakPresetTitleBox.Text = b.Title;
        BreakPresetDurBox.Text = b.DefaultDurationSeconds.ToString();
        BreakPresetSegBox.Text = b.Segment;
        BreakPresetOrderBox.Text = b.OrderSeq.ToString();
        BreakPresetNotesBox.Text = b.Notes;

        foreach (ComboBoxItem item in BreakPresetTypeBox.Items)
        {
            if (string.Equals(item.Content.ToString(), b.BreakType, StringComparison.OrdinalIgnoreCase))
            {
                BreakPresetTypeBox.SelectedItem = item;
                break;
            }
        }

        if (string.IsNullOrEmpty(b.ProgramId))
        {
            BreakTargetProgBox.SelectedIndex = 0; // Global
        }
        else
        {
            var match = BreakTargetProgBox.Items.Cast<object>().FirstOrDefault(x => x.ToString()!.Contains(b.ProgramId));
            if (match != null) BreakTargetProgBox.SelectedItem = match;
        }
    }

    private void NewBreakPreset_Click(object sender, RoutedEventArgs e)
    {
        _selectedBreakPreset = null;
        BreakPresetGrid.SelectedIndex = -1;
        BreakPresetTitleBox.Text = "COMMERCIAL BREAK";
        BreakPresetDurBox.Text = "60";
        BreakPresetSegBox.Text = "A";
        BreakPresetOrderBox.Text = "1";
        BreakPresetNotesBox.Text = "";
        BreakPresetTypeBox.SelectedIndex = 0;
        BreakTargetProgBox.SelectedIndex = 0;
        BreakPresetTitleBox.Focus();
        BreakPresetTitleBox.SelectAll();
    }

    private void SaveBreakPreset_Click(object sender, RoutedEventArgs e)
    {
        var title = BreakPresetTitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            System.Windows.MessageBox.Show("Please enter a preset title.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(BreakPresetDurBox.Text.Trim(), out var dur);
        if (dur <= 0) dur = 60;
        int.TryParse(BreakPresetOrderBox.Text.Trim(), out var order);
        if (order <= 0) order = 1;

        string? progId = null;
        var targetStr = Convert.ToString(BreakTargetProgBox.SelectedItem);
        if (!string.IsNullOrEmpty(targetStr) && !targetStr.StartsWith("GLOBAL", StringComparison.OrdinalIgnoreCase))
        {
            var openBracket = targetStr.LastIndexOf('[');
            var closeBracket = targetStr.LastIndexOf(']');
            if (openBracket >= 0 && closeBracket > openBracket)
            {
                progId = targetStr.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
            }
        }

        var breakType = (BreakPresetTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COMMERCIAL";

        var preset = new NrcsBreakTemplate(
            _selectedBreakPreset?.Id ?? ("BRK-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()),
            progId ?? "",
            title,
            breakType,
            dur,
            string.IsNullOrWhiteSpace(BreakPresetSegBox.Text) ? "A" : BreakPresetSegBox.Text.Trim(),
            order,
            BreakPresetNotesBox.Text.Trim(),
            DateTime.UtcNow
        );

        _store.SaveBreakTemplate(preset);
        _selectedBreakPreset = preset;
        RefreshBreakPresets();
        StatusText.Text = $"Break preset '{preset.Title}' saved.";
    }

    private void DeleteBreakPreset_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBreakPreset is null)
        {
            System.Windows.MessageBox.Show("Select a break preset to delete.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete preset '{_selectedBreakPreset.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _store.DeleteBreakTemplate(_selectedBreakPreset.Id);
            _selectedBreakPreset = null;
            RefreshBreakPresets();
            NewBreakPreset_Click(sender, e);
            StatusText.Text = "Break preset deleted.";
        }
    }

    // --- CATEGORIES CRUD ---
    private void RefreshCategories()
    {
        try
        {
            var cats = _store.ListCategories();
            CategoryGrid.ItemsSource = cats;

            var codes = cats.Select(c => c.Code).ToList();
            StoryCategoryBox.ItemsSource = codes;
            ProgCategoryBox.ItemsSource = codes;
        }
        catch { }
    }

    private void CategoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryGrid.SelectedItem is not NrcsCategory c) return;
        _selectedCategory = c;
        CatCodeBox.Text = c.Code;
        CatNameBox.Text = c.Name;
        CatColorBox.Text = c.ColorHex;
        CatDescBox.Text = c.Description;
        CatDefaultCheck.IsChecked = c.IsDefault;
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        _selectedCategory = null;
        CategoryGrid.SelectedIndex = -1;
        CatCodeBox.Text = "NEW_CAT";
        CatNameBox.Text = "New Category";
        CatColorBox.Text = "#3B82F6";
        CatDescBox.Text = "";
        CatDefaultCheck.IsChecked = false;
        CatCodeBox.Focus();
        CatCodeBox.SelectAll();
    }

    private void SaveCategory_Click(object sender, RoutedEventArgs e)
    {
        var code = CatCodeBox.Text.Trim().ToUpperInvariant();
        var name = CatNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show("Please enter both category code and name.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var cat = new NrcsCategory(
            _selectedCategory?.Id ?? ("CAT-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()),
            name,
            code,
            string.IsNullOrWhiteSpace(CatColorBox.Text) ? "#3B82F6" : CatColorBox.Text.Trim(),
            CatDescBox.Text.Trim(),
            CatDefaultCheck.IsChecked == true,
            DateTime.UtcNow
        );

        _store.SaveCategory(cat);
        _selectedCategory = cat;
        RefreshCategories();
        StatusText.Text = $"Category '{cat.Name}' saved.";
    }

    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCategory is null)
        {
            System.Windows.MessageBox.Show("Select a category to delete.", "Kashtrix NRCS", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = System.Windows.MessageBox.Show($"Are you sure you want to delete category '{_selectedCategory.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
        {
            _store.DeleteCategory(_selectedCategory.Id);
            _selectedCategory = null;
            RefreshCategories();
            NewCategory_Click(sender, e);
            StatusText.Text = "Category deleted.";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => WindowChromeActions.Drag(this, e);
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.Minimize(this);
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowChromeActions.ToggleMaximize(this);
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
