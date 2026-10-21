using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Views;

public partial class KeyframeCurveEditorWindow : Window
{
    private readonly CgKeyframe? _keyframe;
    private double _cp1X = 0.25;
    private double _cp1Y = 0.10;
    private double _cp2X = 0.25;
    private double _cp2Y = 1.00;
    private double _offset = 10.0;

    private enum DragTarget { None, Handle1, Handle2, Point0, Point1 }
    private DragTarget _activeDrag = DragTarget.None;

    public KeyframeCurveEditorWindow() : this(null) { }

    public KeyframeCurveEditorWindow(CgKeyframe? keyframe)
    {
        InitializeComponent();
        WindowChromeActions.ApplyCleanBorder(this);
        _keyframe = keyframe;

        if (_keyframe != null)
        {
            var parsedFromEase = false;
            var ease = _keyframe.Ease?.Trim().ToLowerInvariant() ?? "";
            if (ease.StartsWith("cubic-bezier(") && ease.EndsWith(")"))
            {
                var inner = ease[13..^1];
                var parts = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 4 &&
                    double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var p1x) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var p1y) &&
                    double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var p2x) &&
                    double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var p2y))
                {
                    _cp1X = Math.Clamp(p1x, 0.0, 1.0);
                    _cp1Y = Math.Clamp(p1y, -0.6, 1.6);
                    _cp2X = Math.Clamp(p2x, 0.0, 1.0);
                    _cp2Y = Math.Clamp(p2y, -0.6, 1.6);
                    parsedFromEase = true;
                }
            }
            else if (ease is "linear" or "none")
            {
                _cp1X = 0.0; _cp1Y = 0.0; _cp2X = 1.0; _cp2Y = 1.0;
                parsedFromEase = true;
            }
            else if (ease.Contains(".inout"))
            {
                _cp1X = 0.42; _cp1Y = 0.0; _cp2X = 0.58; _cp2Y = 1.0;
                parsedFromEase = true;
            }
            else if (ease.Contains(".in"))
            {
                _cp1X = 0.42; _cp1Y = 0.0; _cp2X = 1.0; _cp2Y = 1.0;
                parsedFromEase = true;
            }
            else if (ease.Contains(".out"))
            {
                _cp1X = 0.0; _cp1Y = 0.0; _cp2X = 0.58; _cp2Y = 1.0;
                parsedFromEase = true;
            }
            else if (ease.Contains("bounce"))
            {
                _cp1X = 0.175; _cp1Y = 0.885; _cp2X = 0.320; _cp2Y = 1.275;
                parsedFromEase = true;
            }

            if (!parsedFromEase && (_keyframe.Cp1X != 0 || _keyframe.Cp1Y != 0 || _keyframe.Cp2X != 0 || _keyframe.Cp2Y != 0))
            {
                _cp1X = Math.Clamp(_keyframe.Cp1X, 0.0, 1.0);
                _cp1Y = Math.Clamp(_keyframe.Cp1Y, -0.6, 1.6);
                _cp2X = Math.Clamp(_keyframe.Cp2X, 0.0, 1.0);
                _cp2Y = Math.Clamp(_keyframe.Cp2Y, -0.6, 1.6);
            }

            _offset = _keyframe.SpeedOffset != 0 ? _keyframe.SpeedOffset : 10.0;
            KeyframeInfoText.Text = $"Keyframe at {_keyframe.TimeSeconds:0.00}s ({(_keyframe.TargetProperty ?? "All")})";
        }

        OffsetTextBox.Text = _offset.ToString("0", CultureInfo.InvariantCulture);
        Loaded += (s, e) => UpdateVisuals();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void CurveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateVisuals();
    }

    private (double left, double bottom, double spanX, double spanY) GetCanvasBounds()
    {
        double w = CurveCanvas.ActualWidth > 0 ? CurveCanvas.ActualWidth : 760;
        double h = CurveCanvas.ActualHeight > 0 ? CurveCanvas.ActualHeight : 400;

        double marginX = 80;
        double marginY = 70;

        double left = marginX;
        double bottom = h - marginY;
        double spanX = Math.Max(50, w - marginX * 2);
        double spanY = Math.Max(50, h - marginY * 2);

        return (left, bottom, spanX, spanY);
    }

    private Point NormalizedToCanvas(double normX, double normY)
    {
        var (left, bottom, spanX, spanY) = GetCanvasBounds();
        return new Point(left + normX * spanX, bottom - normY * spanY);
    }

    private (double normX, double normY) CanvasToNormalized(Point canvasPt)
    {
        var (left, bottom, spanX, spanY) = GetCanvasBounds();
        double nx = (canvasPt.X - left) / spanX;
        double ny = (bottom - canvasPt.Y) / spanY;
        return (nx, ny);
    }

    private void UpdateVisuals()
    {
        var p0 = NormalizedToCanvas(0, 0);
        var p1 = NormalizedToCanvas(1, 1);
        var h1 = NormalizedToCanvas(_cp1X, _cp1Y);
        var h2 = NormalizedToCanvas(_cp2X, _cp2Y);

        // Path
        CurveFigure.StartPoint = p0;
        CurveBezier.Point1 = h1;
        CurveBezier.Point2 = h2;
        CurveBezier.Point3 = p1;

        // Tangent Lines
        TangentLine1.X1 = p0.X;
        TangentLine1.Y1 = p0.Y;
        TangentLine1.X2 = h1.X;
        TangentLine1.Y2 = h1.Y;

        TangentLine2.X1 = p1.X;
        TangentLine2.Y1 = p1.Y;
        TangentLine2.X2 = h2.X;
        TangentLine2.Y2 = h2.Y;

        // Keyframe points (center 14x14)
        Canvas.SetLeft(Point0, p0.X - Point0.Width / 2);
        Canvas.SetTop(Point0, p0.Y - Point0.Height / 2);

        Canvas.SetLeft(Point1, p1.X - Point1.Width / 2);
        Canvas.SetTop(Point1, p1.Y - Point1.Height / 2);

        // Handles (center 16x16)
        Canvas.SetLeft(Handle1, h1.X - Handle1.Width / 2);
        Canvas.SetTop(Handle1, h1.Y - Handle1.Height / 2);

        Canvas.SetLeft(Handle2, h2.X - Handle2.Width / 2);
        Canvas.SetTop(Handle2, h2.Y - Handle2.Height / 2);

        CurveExpressionText.Text = string.Create(CultureInfo.InvariantCulture, $"cubic-bezier({_cp1X:0.00}, {_cp1Y:0.00}, {_cp2X:0.00}, {_cp2Y:0.00})");
    }

    private void Point0_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _activeDrag = DragTarget.Point0;
        CurveCanvas.CaptureMouse();
    }

    private void Point1_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _activeDrag = DragTarget.Point1;
        CurveCanvas.CaptureMouse();
    }

    private void Handle1_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _activeDrag = DragTarget.Handle1;
        CurveCanvas.CaptureMouse();
    }

    private void Handle2_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _activeDrag = DragTarget.Handle2;
        CurveCanvas.CaptureMouse();
    }

    private void CurveCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_activeDrag == DragTarget.None) return;

        Point pt = e.GetPosition(CurveCanvas);
        var (nx, ny) = CanvasToNormalized(pt);

        if (_activeDrag == DragTarget.Handle1)
        {
            _cp1X = Math.Clamp(nx, 0.0, 1.0);
            _cp1Y = Math.Clamp(ny, -0.6, 1.6);
            UpdateVisuals();
        }
        else if (_activeDrag == DragTarget.Handle2)
        {
            _cp2X = Math.Clamp(nx, 0.0, 1.0);
            _cp2Y = Math.Clamp(ny, -0.6, 1.6);
            UpdateVisuals();
        }
    }

    private void CurveCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_activeDrag != DragTarget.None)
        {
            _activeDrag = DragTarget.None;
            CurveCanvas.ReleaseMouseCapture();
        }
    }

    private void OffsetDec_Click(object sender, RoutedEventArgs e)
    {
        _offset = Math.Max(0, _offset - 1.0);
        OffsetTextBox.Text = _offset.ToString("0", CultureInfo.InvariantCulture);
    }

    private void OffsetInc_Click(object sender, RoutedEventArgs e)
    {
        _offset += 1.0;
        OffsetTextBox.Text = _offset.ToString("0", CultureInfo.InvariantCulture);
    }

    private void OffsetTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(OffsetTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
        {
            _offset = Math.Max(0, val);
        }
        OffsetTextBox.Text = _offset.ToString("0", CultureInfo.InvariantCulture);
    }

    private void PresetLinear_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.0; _cp1Y = 0.0;
        _cp2X = 1.0; _cp2Y = 1.0;
        UpdateVisuals();
    }

    private void PresetEaseIn_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.42; _cp1Y = 0.0;
        _cp2X = 1.0; _cp2Y = 1.0;
        UpdateVisuals();
    }

    private void PresetEaseOut_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.0; _cp1Y = 0.0;
        _cp2X = 0.58; _cp2Y = 1.0;
        UpdateVisuals();
    }

    private void PresetEaseInOut_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.42; _cp1Y = 0.0;
        _cp2X = 0.58; _cp2Y = 1.0;
        UpdateVisuals();
    }

    private void PresetSmooth_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.25; _cp1Y = 0.10;
        _cp2X = 0.25; _cp2Y = 1.00;
        UpdateVisuals();
    }

    private void PresetBounce_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.175; _cp1Y = 0.885;
        _cp2X = 0.320; _cp2Y = 1.275;
        UpdateVisuals();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _cp1X = 0.25; _cp1Y = 0.10;
        _cp2X = 0.25; _cp2Y = 1.00;
        _offset = 10.0;
        OffsetTextBox.Text = _offset.ToString("0", CultureInfo.InvariantCulture);
        UpdateVisuals();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_keyframe != null)
        {
            _keyframe.Cp1X = _cp1X;
            _keyframe.Cp1Y = _cp1Y;
            _keyframe.Cp2X = _cp2X;
            _keyframe.Cp2Y = _cp2Y;
            _keyframe.SpeedOffset = _offset;
            _keyframe.Ease = string.Create(CultureInfo.InvariantCulture, $"cubic-bezier({_cp1X:0.00},{_cp1Y:0.00},{_cp2X:0.00},{_cp2Y:0.00})");
        }
        DialogResult = true;
        Close();
    }
}
