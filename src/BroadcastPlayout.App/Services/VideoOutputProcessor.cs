using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

public readonly record struct VideoOutputAdjustments(
    bool Enabled,
    int Brightness,
    int Contrast,
    int Saturation,
    double Gamma,
    int HueDegrees,
    double ExposureStops,
    int Temperature,
    int Tint,
    int Sharpness,
    int BlackLevel,
    int WhiteLevel,
    int RedGainPercent,
    int GreenGainPercent,
    int BlueGainPercent)
{
    public bool IsNeutral => !Enabled ||
        (Brightness == 0 && Contrast == 100 && Saturation == 100 && Math.Abs(Gamma - 1.0) < 0.0001 &&
         HueDegrees == 0 && Math.Abs(ExposureStops) < 0.0001 && Temperature == 0 && Tint == 0 && Sharpness == 0 &&
         BlackLevel == 0 && WhiteLevel == 0 && RedGainPercent == 100 && GreenGainPercent == 100 && BlueGainPercent == 100);
}

/// <summary>
/// Final-program BGRA processor. The source frame is never mutated: when processing is enabled a new
/// frame buffer is returned, so Preview/source caching and CG source frames remain stable.
/// </summary>
public static class VideoOutputProcessor
{
    public static VideoFrameData Apply(VideoFrameData frame, VideoOutputAdjustments a)
    {
        if (a.IsNeutral || frame.Bgra.Length == 0 || frame.Width <= 0 || frame.Height <= 0) return frame;

        var src = frame.Bgra;
        var dst = (byte[])src.Clone();
        var stride = Math.Max(frame.Stride, frame.Width * 4);

        var brightness = Math.Clamp(a.Brightness, -100, 100) * 2.55;
        var contrast = Math.Clamp(a.Contrast, 0, 200) / 100.0;
        var saturation = Math.Clamp(a.Saturation, 0, 200) / 100.0;
        var gamma = Math.Clamp(a.Gamma, 0.10, 4.0);
        var invGamma = 1.0 / gamma;
        var exposure = Math.Pow(2.0, Math.Clamp(a.ExposureStops, -4.0, 4.0));
        var temp = Math.Clamp(a.Temperature, -100, 100) / 100.0;
        var tint = Math.Clamp(a.Tint, -100, 100) / 100.0;
        var black = Math.Clamp(a.BlackLevel, -100, 100) * 0.45;
        var white = Math.Clamp(a.WhiteLevel, -100, 100) * 0.45;
        var redGain = Math.Clamp(a.RedGainPercent, 0, 200) / 100.0;
        var greenGain = Math.Clamp(a.GreenGainPercent, 0, 200) / 100.0;
        var blueGain = Math.Clamp(a.BlueGainPercent, 0, 200) / 100.0;

        // Temperature/tint are intentionally conservative broadcast trims rather than aggressive color grading.
        redGain *= 1.0 + temp * 0.18 + tint * 0.08;
        blueGain *= 1.0 - temp * 0.18 + tint * 0.08;
        greenGain *= 1.0 - tint * 0.12;

        var hue = Math.Clamp(a.HueDegrees, -180, 180) * Math.PI / 180.0;
        var cosH = Math.Cos(hue);
        var sinH = Math.Sin(hue);
        var redLut = BuildGradeLut(redGain, exposure, contrast, brightness, black, white, invGamma);
        var greenLut = BuildGradeLut(greenGain, exposure, contrast, brightness, black, white, invGamma);
        var blueLut = BuildGradeLut(blueGain, exposure, contrast, brightness, black, white, invGamma);

        // The overwhelmingly common broadcast correction path has neutral hue/saturation.
        // It now becomes three 256-entry table lookups per pixel instead of repeated floating
        // point YIQ transforms and Math.Pow calls at 25/50/60 fps.
        var simpleColorPath = Math.Abs(hue) < 0.0001 && Math.Abs(saturation - 1.0) < 0.0001;
        for (var y = 0; y < frame.Height; y++)
        {
            var row = y * stride;
            var rowEnd = Math.Min(src.Length, row + frame.Width * 4);
            if (simpleColorPath)
            {
                for (var i = row; i + 3 < rowEnd; i += 4)
                {
                    dst[i + 0] = blueLut[src[i + 0]];
                    dst[i + 1] = greenLut[src[i + 1]];
                    dst[i + 2] = redLut[src[i + 2]];
                    dst[i + 3] = src[i + 3];
                }
                continue;
            }

            for (var i = row; i + 3 < rowEnd; i += 4)
            {
                var b = src[i + 0];
                var g = src[i + 1];
                var r = src[i + 2];
                var yy = 0.299 * r + 0.587 * g + 0.114 * b;
                var ii = 0.596 * r - 0.274 * g - 0.322 * b;
                var qq = 0.211 * r - 0.523 * g + 0.312 * b;
                var i2 = (ii * cosH - qq * sinH) * saturation;
                var q2 = (ii * sinH + qq * cosH) * saturation;
                var rr = yy + 0.956 * i2 + 0.621 * q2;
                var gg = yy - 0.272 * i2 - 0.647 * q2;
                var bb = yy - 1.106 * i2 + 1.703 * q2;

                dst[i + 0] = blueLut[ClampToByteIndex(bb)];
                dst[i + 1] = greenLut[ClampToByteIndex(gg)];
                dst[i + 2] = redLut[ClampToByteIndex(rr)];
                dst[i + 3] = src[i + 3];
            }
        }

        var sharpness = Math.Clamp(a.Sharpness, 0, 100);
        if (sharpness > 0 && frame.Width > 2 && frame.Height > 2)
            ApplySharpenInPlace(dst, frame.Width, frame.Height, stride, sharpness / 100.0);

        return new VideoFrameData(dst, frame.Width, frame.Height, frame.Stride, frame.PtsSeconds, frame.FrameRateNumerator, frame.FrameRateDenominator);
    }

    private static byte[] BuildGradeLut(double gain, double exposure, double contrast, double brightness, double black, double white, double invGamma)
    {
        var lut = new byte[256];
        for (var i = 0; i < lut.Length; i++)
            lut[i] = ToByte(Grade(i, gain, exposure, contrast, brightness, black, white, invGamma));
        return lut;
    }

    private static int ClampToByteIndex(double value)
        => value <= 0 ? 0 : value >= 255 ? 255 : (int)(value + 0.5);

    private static double Grade(double v, double gain, double exposure, double contrast, double brightness, double black, double white, double invGamma)
    {
        v *= gain * exposure;
        v = (v - 127.5) * contrast + 127.5 + brightness;
        // Black/white trims are endpoints, not just another brightness control.
        v = (v - black) * (255.0 / Math.Max(1.0, 255.0 + white - black));
        v = Math.Clamp(v, 0.0, 255.0);
        if (Math.Abs(invGamma - 1.0) > 0.0001)
            v = 255.0 * Math.Pow(v / 255.0, invGamma);
        return v;
    }

    private static byte ToByte(double v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);

    private static void ApplySharpenInPlace(byte[] buffer, int width, int height, int stride, double strength)
    {
        // One light unsharp pass. Keep a copy so the kernel never reads already-modified neighbours.
        var src = (byte[])buffer.Clone();
        var amount = 0.65 * strength;
        for (var y = 1; y < height - 1; y++)
        {
            var row = y * stride;
            for (var x = 1; x < width - 1; x++)
            {
                var p = row + x * 4;
                for (var c = 0; c < 3; c++)
                {
                    var center = src[p + c];
                    var blur = (src[p - 4 + c] + src[p + 4 + c] + src[p - stride + c] + src[p + stride + c]) * 0.25;
                    buffer[p + c] = ToByte(center + (center - blur) * amount);
                }
            }
        }
    }
}
