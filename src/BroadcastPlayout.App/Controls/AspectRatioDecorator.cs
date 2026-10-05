using System;
using System.Windows;
using System.Windows.Controls;

namespace BroadcastPlayout.Controls;

public sealed class AspectRatioDecorator : Decorator
{
    public static readonly DependencyProperty RatioProperty = DependencyProperty.Register(
        nameof(Ratio), typeof(double), typeof(AspectRatioDecorator),
        new FrameworkPropertyMetadata(16.0 / 9.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double Ratio
    {
        get => (double)GetValue(RatioProperty);
        set => SetValue(RatioProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child is null) return new Size();
        var size = Fit(constraint);
        Child.Measure(size);
        return size;
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        if (Child is null) return arrangeSize;
        var size = Fit(arrangeSize);
        var x = Math.Max(0, (arrangeSize.Width - size.Width) / 2.0);
        var y = Math.Max(0, (arrangeSize.Height - size.Height) / 2.0);
        Child.Arrange(new Rect(new Point(x, y), size));
        return arrangeSize;
    }

    private Size Fit(Size available)
    {
        var ratio = Ratio <= 0 ? 16.0 / 9.0 : Ratio;
        var width = double.IsInfinity(available.Width) ? 1920 : available.Width;
        var height = double.IsInfinity(available.Height) ? 1080 : available.Height;
        if (width <= 0 || height <= 0) return new Size(0, 0);
        if (width / height > ratio) width = height * ratio;
        else height = width / ratio;
        return new Size(width, height);
    }
}
