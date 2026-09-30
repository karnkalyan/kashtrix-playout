namespace BroadcastPlayout.Models;

public sealed record VideoFrameData(byte[] Bgra, int Width, int Height, int Stride, double PtsSeconds, int FrameRateNumerator, int FrameRateDenominator)
{
    public double FramesPerSecond => FrameRateDenominator == 0 ? 0d : (double)FrameRateNumerator / FrameRateDenominator;
}
public sealed record AudioChunk(byte[] Pcm16Stereo48k, double PtsSeconds, int SampleRate = 48000, int Channels = 2, int BitsPerSample = 16);
public sealed record MediaInfo(TimeSpan Duration, int Width, int Height, double FramesPerSecond, string VideoCodec, bool HasAudio);
