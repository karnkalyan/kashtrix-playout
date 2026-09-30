using System.Runtime.InteropServices;
using BroadcastPlayout.Models;
using FFmpeg.AutoGen.Abstractions;

namespace BroadcastPlayout.Ffmpeg;

/// <summary>
/// Reusable BGRA-to-BGRA scaler for output adapters. One instance must be used from one thread at a time.
/// </summary>
public sealed unsafe class BgraFrameScaler : IDisposable
{
    private SwsContext* _ctx;
    private int _srcW, _srcH, _dstW, _dstH;
    private IntPtr _dstBuffer;
    private int _dstBufferSize;
    private byte_ptr4 _dstData;
    private int4 _dstLinesize;

    public VideoFrameData Scale(VideoFrameData source, int targetWidth, int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(source);
        targetWidth = Math.Max(16, targetWidth);
        targetHeight = Math.Max(16, targetHeight);

        if (source.Width == targetWidth && source.Height == targetHeight)
            return source;

        EnsureContext(source.Width, source.Height, targetWidth, targetHeight);

        fixed (byte* sourcePtr = source.Bgra)
        {
            var srcData = new byte_ptr8();
            var srcStride = new int8();
            srcData[0] = sourcePtr;
            srcStride[0] = source.Stride;

            var scaled = ffmpeg.sws_scale(
                _ctx,
                srcData,
                srcStride,
                0,
                source.Height,
                _dstData,
                _dstLinesize);

            if (scaled <= 0)
                throw new InvalidOperationException("FFmpeg sws_scale failed while resizing the output frame.");
        }

        var managed = new byte[_dstBufferSize];
        Marshal.Copy(_dstBuffer, managed, 0, managed.Length);
        return new VideoFrameData(
            managed,
            targetWidth,
            targetHeight,
            _dstLinesize[0],
            source.PtsSeconds,
            source.FrameRateNumerator,
            source.FrameRateDenominator);
    }

    private void EnsureContext(int srcW, int srcH, int dstW, int dstH)
    {
        FfmpegRuntime.EnsureReady();
        if (_ctx != null && _srcW == srcW && _srcH == srcH && _dstW == dstW && _dstH == dstH)
            return;

        ReleaseContext();

        _ctx = ffmpeg.sws_getContext(
            srcW, srcH, AVPixelFormat.AV_PIX_FMT_BGRA,
            dstW, dstH, AVPixelFormat.AV_PIX_FMT_BGRA,
            (int)SwsFlags.SWS_BILINEAR,
            null, null, null);

        if (_ctx == null)
            throw new InvalidOperationException("FFmpeg sws_getContext failed for output scaling.");

        _dstBufferSize = ffmpeg.av_image_get_buffer_size(
            AVPixelFormat.AV_PIX_FMT_BGRA, dstW, dstH, 1).ThrowIfError();
        _dstBuffer = Marshal.AllocHGlobal(_dstBufferSize);
        _dstData = new byte_ptr4();
        _dstLinesize = new int4();
        ffmpeg.av_image_fill_arrays(
            ref _dstData,
            ref _dstLinesize,
            (byte*)_dstBuffer,
            AVPixelFormat.AV_PIX_FMT_BGRA,
            dstW,
            dstH,
            1).ThrowIfError();

        _srcW = srcW;
        _srcH = srcH;
        _dstW = dstW;
        _dstH = dstH;
    }

    private void ReleaseContext()
    {
        if (_dstBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_dstBuffer);
            _dstBuffer = IntPtr.Zero;
        }

        if (_ctx != null)
        {
            ffmpeg.sws_freeContext(_ctx);
            _ctx = null;
        }

        _dstBufferSize = 0;
        _srcW = _srcH = _dstW = _dstH = 0;
    }

    public void Dispose() => ReleaseContext();
}
