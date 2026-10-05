using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BroadcastPlayout.Views;

public sealed record FileSystemEntryItem(
    string Name,
    string FullPath,
    bool IsDirectory,
    string DateModified,
    string TypeName,
    string SizeText,
    string Glyph,
    Brush GlyphColor
);

public partial class KashtrixFileDialog : Window
{
    private readonly List<string> _history = [];
    private int _historyIndex = -1;
    private string _currentDirectory = "";
    private readonly List<FilterOption> _filters = [];

    public sealed record FilterOption(string Display, string[] Extensions);

    public bool IsSaveMode { get; set; }
    public bool Multiselect { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string[] FileNames { get; private set; } = [];
    public string InitialDirectory { get; set; } = string.Empty;
    public string DefaultExt { get; set; } = string.Empty;
    public string Filter { get => string.Empty; set => SetFilter(value); }

    private static readonly Brush FolderBrush = new SolidColorBrush(Color.FromRgb(177, 107, 228));
    private static readonly Brush MediaBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
    private static readonly Brush DocBrush = new SolidColorBrush(Color.FromRgb(229, 182, 76));

    public KashtrixFileDialog()
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);
        Loaded += KashtrixFileDialog_Loaded;
    }

    public void SetFilter(string filterString)
    {
        _filters.Clear();
        if (string.IsNullOrWhiteSpace(filterString))
        {
            _filters.Add(new FilterOption("All Files (*.*)", ["*.*"]));
        }
        else
        {
            var parts = filterString.Split('|');
            for (var i = 0; i < parts.Length; i += 2)
            {
                var label = parts[i].Trim();
                var pattern = (i + 1 < parts.Length) ? parts[i + 1].Trim() : "*.*";
                var exts = pattern.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _filters.Add(new FilterOption($"{label} ({pattern})", exts));
            }
        }
    }

    private void KashtrixFileDialog_Loaded(object sender, RoutedEventArgs e)
    {
        ActionButton.Content = IsSaveMode ? "SAVE" : "OPEN";
        TitleText.Text = IsSaveMode ? (string.IsNullOrWhiteSpace(Title) ? "SAVE FILE" : Title.ToUpperInvariant())
                                    : (string.IsNullOrWhiteSpace(Title) ? "OPEN FILE" : Title.ToUpperInvariant());

        if (Multiselect && !IsSaveMode)
        {
            FilesListView.SelectionMode = SelectionMode.Extended;
        }

        // Setup filter combobox
        FilterComboBox.ItemsSource = _filters.Select(x => x.Display).ToList();
        if (FilterComboBox.Items.Count > 0) FilterComboBox.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(FileName))
        {
            FileNameBox.Text = Path.GetFileName(FileName);
        }

        PopulateShortcuts();
        PopulateDrives();

        var startDir = !string.IsNullOrWhiteSpace(InitialDirectory) && Directory.Exists(InitialDirectory)
            ? InitialDirectory
            : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        if (!Directory.Exists(startDir))
        {
            startDir = AppContext.BaseDirectory;
        }

        NavigateTo(startDir, addToHistory: true);
    }

    private void PopulateShortcuts()
    {
        var shortcuts = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };

        ShortcutsList.ItemsSource = shortcuts.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private bool _suppressDriveCombo;

    private void PopulateDrives()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => d.RootDirectory.FullName)
            .ToList();
        DrivesList.ItemsSource = drives;

        var entries = new List<string>();
        foreach (var f in new[]
        {
            Environment.SpecialFolder.MyVideos, Environment.SpecialFolder.MyMusic, Environment.SpecialFolder.MyPictures,
            Environment.SpecialFolder.Desktop, Environment.SpecialFolder.MyDocuments
        })
        {
            var p = Environment.GetFolderPath(f);
            if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) entries.Add(p);
        }
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (Directory.Exists(downloads)) entries.Add(downloads);
        entries.AddRange(drives);
        _suppressDriveCombo = true;
        DriveFolderComboBox.ItemsSource = entries.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        DriveFolderComboBox.SelectedIndex = -1;
        _suppressDriveCombo = false;
    }

    private void DriveFolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDriveCombo) return;
        if (DriveFolderComboBox.SelectedItem is string path) NavigateTo(path);
    }

    public void NavigateTo(string path, bool addToHistory = true)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

        try
        {
            _currentDirectory = Path.GetFullPath(path);
            CurrentPathBox.Text = _currentDirectory;

            if (addToHistory)
            {
                if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
                }
                _history.Add(_currentDirectory);
                _historyIndex = _history.Count - 1;
            }

            RefreshFileList();
        }
        catch { }
    }

    private void RefreshFileList()
    {
        if (string.IsNullOrWhiteSpace(_currentDirectory) || !Directory.Exists(_currentDirectory)) return;

        try
        {
            var dir = new DirectoryInfo(_currentDirectory);
            var query = SearchBox.Text?.Trim() ?? "";

            var selectedExts = FilterComboBox.SelectedIndex >= 0 && FilterComboBox.SelectedIndex < _filters.Count
                ? _filters[FilterComboBox.SelectedIndex].Extensions
                : ["*.*"];

            var items = new List<FileSystemEntryItem>();

            // Folders
            foreach (var d in dir.GetDirectories())
            {
                if ((d.Attributes & FileAttributes.Hidden) != 0) continue;
                if (!string.IsNullOrWhiteSpace(query) && !d.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                items.Add(new FileSystemEntryItem(
                    d.Name,
                    d.FullName,
                    true,
                    d.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    "File folder",
                    "",
                    "📁",
                    FolderBrush
                ));
            }

            // Files
            foreach (var f in dir.GetFiles())
            {
                if ((f.Attributes & FileAttributes.Hidden) != 0) continue;
                if (!string.IsNullOrWhiteSpace(query) && !f.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                if (!MatchesFilter(f.Extension, selectedExts)) continue;

                var isMedia = IsMediaExtension(f.Extension);
                items.Add(new FileSystemEntryItem(
                    f.Name,
                    f.FullName,
                    false,
                    f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    f.Extension.TrimStart('.').ToUpperInvariant() + " File",
                    FormatSize(f.Length),
                    isMedia ? "🎬" : "📄",
                    isMedia ? MediaBrush : DocBrush
                ));
            }

            FilesListView.ItemsSource = items;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not list directory: {ex.Message}", "Kashtrix File Explorer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static bool MatchesFilter(string ext, string[] patterns)
    {
        if (patterns.Length == 0 || patterns.Any(p => p == "*.*" || p == "*")) return true;
        foreach (var p in patterns)
        {
            var clean = p.TrimStart('*');
            if (ext.Equals(clean, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static bool IsMediaExtension(string ext)
    {
        var e = ext.ToLowerInvariant();
        return e is ".mp4" or ".mov" or ".mxf" or ".mkv" or ".avi" or ".ts" or ".m2ts" or ".mpg" or ".mpeg" or ".webm" or ".mp3" or ".wav" or ".aac" or ".png" or ".jpg" or ".jpeg";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):0.#} MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.##} GB";
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentDirectory)) return;
        var parent = Directory.GetParent(_currentDirectory);
        if (parent is not null && parent.Exists)
        {
            NavigateTo(parent.FullName);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_historyIndex > 0)
        {
            _historyIndex--;
            NavigateTo(_history[_historyIndex], addToHistory: false);
        }
    }

    private void CurrentPathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var p = CurrentPathBox.Text.Trim();
            if (Directory.Exists(p)) NavigateTo(p);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshFileList();

    private void ShortcutsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ShortcutsList.SelectedItem is string path) NavigateTo(path);
    }

    private void DrivesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivesList.SelectedItem is string drive) NavigateTo(drive);
    }

    private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FilesListView.SelectedItem is FileSystemEntryItem item)
        {
            if (item.IsDirectory)
            {
                NavigateTo(item.FullPath);
            }
            else
            {
                ConfirmSelection(item.FullPath);
            }
        }
    }

    private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilesListView.SelectedItems.Count > 1)
        {
            var names = FilesListView.SelectedItems.OfType<FileSystemEntryItem>().Where(x => !x.IsDirectory).Select(x => $"\"{x.Name}\"");
            FileNameBox.Text = string.Join(" ", names);
        }
        else if (FilesListView.SelectedItem is FileSystemEntryItem item && !item.IsDirectory)
        {
            FileNameBox.Text = item.Name;
        }
    }

    private void FileNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Action_Click(sender, e);
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshFileList();

    private void Action_Click(object sender, RoutedEventArgs e)
    {
        if (Multiselect && FilesListView.SelectedItems.Count > 1)
        {
            var selectedFiles = FilesListView.SelectedItems.OfType<FileSystemEntryItem>()
                .Where(x => !x.IsDirectory)
                .Select(x => x.FullPath)
                .ToArray();

            if (selectedFiles.Length > 0)
            {
                FileNames = selectedFiles;
                FileName = selectedFiles[0];
                DialogResult = true;
                Close();
                return;
            }
        }

        var text = FileNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // Check if user entered a full path
        string candidate = Path.IsPathRooted(text) ? text : Path.Combine(_currentDirectory, text);

        if (Directory.Exists(candidate))
        {
            NavigateTo(candidate);
            return;
        }

        if (IsSaveMode)
        {
            if (string.IsNullOrWhiteSpace(Path.GetExtension(candidate)) && !string.IsNullOrWhiteSpace(DefaultExt))
            {
                candidate += DefaultExt.StartsWith('.') ? DefaultExt : "." + DefaultExt;
            }
            FileName = candidate;
            FileNames = [candidate];
            DialogResult = true;
            Close();
        }
        else
        {
            if (File.Exists(candidate))
            {
                FileName = candidate;
                FileNames = [candidate];
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show($"File '{candidate}' does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void ConfirmSelection(string fullPath)
    {
        FileName = fullPath;
        FileNames = [fullPath];
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2) Maximize_Click(sender, e);
        else try { DragMove(); } catch { }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        if (MaxGlyph != null) MaxGlyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class KashtrixSaveFileDialog : KashtrixFileDialog
{
    public KashtrixSaveFileDialog()
    {
        IsSaveMode = true;
    }
}
