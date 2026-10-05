using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace BroadcastPlayout.Views;

/// <summary>
/// Professional unified Kashtrix broadcast theme window used for all dock-out panels,
/// confidence monitors, and secondary dialogs. Matches the K-Mix / Kashtrix Program Out UI.
/// </summary>
public class KashtrixDockWindow : Window
{
    private readonly Border _titlebar;
    private readonly Border _footer;
    private readonly TextBlock _titleText;
    private readonly Border _badgeBorder;
    private readonly TextBlock _badgeText;
    private readonly TextBlock _footerText;
    private readonly Grid _contentHost;
    private bool _fullscreen;
    private Rect _prevBounds;

    public KashtrixDockWindow(string title, string badge, UIElement? content = null, string footer = "")
        : this(title, badge, Color.FromRgb(140, 90, 220), content, footer)
    {
    }

    public KashtrixDockWindow(string title, string badge, Color badgeColor, UIElement? content = null, string footer = "")
    {
        Title = title;
        Width = 1100;
        Height = 650;
        MinWidth = 420;
        MinHeight = 260;
        Background = new SolidColorBrush(Color.FromRgb(9, 13, 17));
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = true;

        try
        {
            var res = Application.GetResourceStream(new Uri("pack://application:,,,/Kashtrix.Playout;component/Branding/kashtrix-playout.ico", UriKind.Absolute));
            if (res?.Stream is not null) Icon = BitmapFrame.Create(res.Stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        }
        catch { }

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false
        });

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) }); // Titlebar
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = string.IsNullOrWhiteSpace(footer) ? GridLength.Auto : new GridLength(30) }); // Footer

        // 1. Titlebar
        _titlebar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(14, 20, 29)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(28, 39, 51)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(12, 0, 8, 0)
        };

        var tg = new Grid();
        var leftStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        _badgeBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, badgeColor.R, badgeColor.G, badgeColor.B)),
            BorderBrush = new SolidColorBrush(badgeColor),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 0, 10, 0)
        };
        _badgeText = new TextBlock
        {
            Text = badge.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(badgeColor)
        };
        _badgeBorder.Child = _badgeText;
        leftStack.Children.Add(_badgeBorder);

        _titleText = new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.FromRgb(236, 239, 241)),
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        leftStack.Children.Add(_titleText);
        tg.Children.Add(leftStack);

        var wins = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

        var fullBtn = CreateChromeButton("⛶", "Toggle Fullscreen (F11)", (_, _) => ToggleFullscreen());
        var minBtn = CreateChromeButton("—", "Minimize", (_, _) => WindowState = WindowState.Minimized);
        var maxBtn = CreateChromeButton("□", "Maximize / Restore", (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        var closeBtn = CreateChromeButton("✕", "Close", (_, _) => Close(), isClose: true);

        wins.Children.Add(fullBtn);
        wins.Children.Add(minBtn);
        wins.Children.Add(maxBtn);
        wins.Children.Add(closeBtn);
        tg.Children.Add(wins);
        _titlebar.Child = tg;

        _titlebar.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else
                try { DragMove(); } catch { }
        };

        Grid.SetRow(_titlebar, 0);
        rootGrid.Children.Add(_titlebar);

        // 2. Content Host
        _contentHost = new Grid { Background = Brushes.Black, ClipToBounds = true };
        if (content != null) _contentHost.Children.Add(content);
        Grid.SetRow(_contentHost, 1);
        rootGrid.Children.Add(_contentHost);

        // 3. Footer
        _footer = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(8, 12, 18)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(100, badgeColor.R, badgeColor.G, badgeColor.B)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(14, 0, 14, 0),
            Visibility = string.IsNullOrWhiteSpace(footer) ? Visibility.Collapsed : Visibility.Visible
        };
        _footerText = new TextBlock
        {
            Text = footer,
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(Color.FromRgb(140, 160, 180)),
            VerticalAlignment = VerticalAlignment.Center
        };
        _footer.Child = _footerText;
        Grid.SetRow(_footer, 2);
        rootGrid.Children.Add(_footer);

        var outerBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 52, 69)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(9, 13, 17)),
            Child = rootGrid
        };
        Content = outerBorder;

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F11) { ToggleFullscreen(); e.Handled = true; }
            else if (e.Key == Key.Escape && _fullscreen) { ToggleFullscreen(); e.Handled = true; }
        };
    }

    public Grid ContentHost => _contentHost;

    public void SetDockContent(UIElement? content)
    {
        _contentHost.Children.Clear();
        if (content != null) _contentHost.Children.Add(content);
    }

    public void SetFooterText(string text)
    {
        _footerText.Text = text;
        _footer.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private Button CreateChromeButton(string glyph, string tooltip, RoutedEventHandler handler, bool isClose = false)
    {
        var btn = new Button
        {
            Content = new TextBlock { Text = glyph, FontSize = isClose ? 11 : 12, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            Width = 32,
            Height = 26,
            Background = Brushes.Transparent,
            Foreground = isClose ? new SolidColorBrush(Color.FromRgb(255, 120, 120)) : new SolidColorBrush(Color.FromRgb(200, 210, 220)),
            BorderThickness = new Thickness(0),
            ToolTip = tooltip,
            Cursor = Cursors.Hand,
            Focusable = false
        };
        btn.Click += handler;
        return btn;
    }

    public void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _fullscreen = true;
            _prevBounds = new Rect(Left, Top, Width, Height);
            _titlebar.Visibility = Visibility.Collapsed;
            _footer.Visibility = Visibility.Collapsed;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
        }
        else
        {
            _fullscreen = false;
            _titlebar.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(_footerText.Text)) _footer.Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
            Left = _prevBounds.Left;
            Top = _prevBounds.Top;
            Width = _prevBounds.Width;
            Height = _prevBounds.Height;
        }
    }
}
