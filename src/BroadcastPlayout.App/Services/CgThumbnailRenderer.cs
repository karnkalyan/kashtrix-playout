using System.Globalization;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

/// <summary>
/// Produces thumbnails with the same compositor used by the CG Editor and Program output.
/// This keeps catalog/layer cards faithful to the authored composition instead of drawing
/// approximate colored rectangles.
/// </summary>
public static class CgThumbnailRenderer
{
    public static ImageSource? RenderProject(CgProject project, int width = 320, int height = 180)
    {
        try
        {
            using var compositor = new CgCompositor();
            var timeline = ResolveRepresentativeTime(project);
            var frame = compositor.RenderProjectSurface(project, Math.Max(2, width), Math.Max(2, height), timeline);
            var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, frame.Bgra, frame.Stride);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource? RenderLayer(CgLayer source, int width = 96, int height = 54)
    {
        try
        {
            var clone = JsonSerializer.Deserialize<CgLayer>(JsonSerializer.Serialize(source));
            if (clone is null) return null;

            var sourceWidth = Math.Max(1, Math.Abs(clone.Width));
            var sourceHeight = Math.Max(1, Math.Abs(clone.Height));
            var scale = Math.Min((width * .84) / sourceWidth, (height * .80) / sourceHeight);
            clone.X = (width - sourceWidth * scale) / 2;
            clone.Y = (height - sourceHeight * scale) / 2;
            clone.Width = sourceWidth * scale;
            clone.Height = sourceHeight * scale;
            clone.StartSeconds = 0;
            clone.EndSeconds = Math.Max(2, clone.AnimationInSeconds + 1);
            clone.Visible = true;
            clone.Opacity = Math.Clamp(double.IsFinite(clone.Opacity) ? clone.Opacity : 1, 0, 1);

            var project = new CgProject
            {
                Name = source.Name + " thumbnail",
                Width = width,
                Height = height,
                FrameRate = 25,
                DurationSeconds = clone.EndSeconds,
                Layers = [clone]
            };
            return RenderProject(project, width, height);
        }
        catch
        {
            return null;
        }
    }

    private static double ResolveRepresentativeTime(CgProject project)
    {
        var visible = project.Layers?.Where(x => x.Visible).ToArray() ?? [];
        if (visible.Length == 0) return 0;
        var intro = visible.Select(x => Math.Max(0, x.AnimationInSeconds)).DefaultIfEmpty(0).Max();
        var firstStart = visible.Select(x => Math.Max(0, x.StartSeconds)).DefaultIfEmpty(0).Min();
        var duration = Math.Max(.1, project.DurationSeconds);
        return Math.Clamp(firstStart + intro + .15, 0, Math.Max(0, duration - .1));
    }
}

public sealed class CgLayerThumbnailConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is CgLayer layer ? CgThumbnailRenderer.RenderLayer(layer) : null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

public sealed class CgGradientPreviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not CgLayer layer) return Brushes.Transparent;
        var rawAngle = double.IsFinite(layer.GradientAngle) ? layer.GradientAngle : 0;
        var angle = (rawAngle % 360 + 360) % 360;
        var radians = angle * Math.PI / 180.0;
        var dx = Math.Cos(radians) * .5;
        var dy = Math.Sin(radians) * .5;
        var brush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(.5 - dx, .5 - dy),
            EndPoint = new System.Windows.Point(.5 + dx, .5 + dy),
            MappingMode = BrushMappingMode.RelativeToBoundingBox
        };
        var stops = (layer.GradientStops ?? []).OrderBy(x => x.Offset).ToArray();
        if (stops.Length >= 2)
        {
            foreach (var stop in stops)
                brush.GradientStops.Add(new GradientStop(ParseColor(stop.Color, Colors.White), Math.Clamp(double.IsFinite(stop.Offset) ? stop.Offset : 0, 0, 1)));
        }
        else
        {
            var primary = layer.Type.Equals("Shape", StringComparison.OrdinalIgnoreCase) ? layer.Background : layer.Fill;
            brush.GradientStops.Add(new GradientStop(ParseColor(primary, Colors.White), 0));
            brush.GradientStops.Add(new GradientStop(ParseColor(layer.GradientColor2, Colors.Black), 1));
        }
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    private static Color ParseColor(string? value, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(value ?? string.Empty); }
        catch { return fallback; }
    }
}
