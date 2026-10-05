using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BroadcastPlayout.Controls;

namespace BroadcastPlayout.Views;

public sealed class ConfidenceMonitorWindow : KashtrixDockWindow
{
    private readonly Func<ImageSource?> _sourceProvider;
    private readonly Image _image;

    public ConfidenceMonitorWindow(string title, Func<ImageSource?> sourceProvider)
        : base(
            title: title,
            badge: title.Contains("Program", StringComparison.OrdinalIgnoreCase) ? "PGM OUT" : "PVW OUT",
            badgeColor: title.Contains("Program", StringComparison.OrdinalIgnoreCase) ? Color.FromRgb(228, 33, 44) : Color.FromRgb(39, 201, 63),
            footer: "1080p · 16:9 TRUE BROADCAST RASTER · REALTIME CONFIDENCE MONITOR"
        )
    {
        _sourceProvider = sourceProvider;
        Width = 1120;
        Height = 680;
        MinWidth = 480;
        MinHeight = 320;

        _image = new Image
        {
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var ratioDecorator = new AspectRatioDecorator
        {
            Ratio = 16.0 / 9.0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = _image
        };

        ContentHost.Children.Add(ratioDecorator);

        Loaded += (_, _) =>
        {
            try { _image.Source = _sourceProvider(); } catch { }
            CompositionTarget.Rendering += OnRendering;
        };

        Closed += (_, _) =>
        {
            CompositionTarget.Rendering -= OnRendering;
        };
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        try
        {
            var current = _sourceProvider();
            if (!ReferenceEquals(_image.Source, current))
            {
                _image.Source = current;
            }
        }
        catch { }
    }
}
