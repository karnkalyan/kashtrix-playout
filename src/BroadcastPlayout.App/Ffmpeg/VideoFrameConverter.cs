using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Ffmpeg;

public sealed unsafe class VideoFrameConverter : IDisposable
{
    private readonly SwsContext* _ctx;
    private readonly IntPtr _buffer;
    private readonly byte_ptr4 _dstData;
    private readonly int4 _dstLinesize;
    private readonly int _width;
    private readonly int _height;
    private readonly int _bufferSize;

    public VideoFrameConverter(int width, int height, AVPixelFormat sourceFormat)
    {
        _width = width; _height = height;
        _ctx = ffmpeg.sws_getContext(width, height, sourceFormat, width, height, AVPixelFormat.AV_PIX_FMT_BGRA,
            (int)SwsFlags.SWS_BILINEAR, null, null, null);
        if (_ctx == null) throw new InvalidOperationException("sws_getContext failed.");
        _bufferSize = ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGRA, width, height, 1).ThrowIfError();
        _buffer = Marshal.AllocHGlobal(_bufferSize);
        _dstData = new byte_ptr4(); _dstLinesize = new int4();
        ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_buffer,
            AVPixelFormat.AV_PIX_FMT_BGRA, width, height, 1).ThrowIfError();
    }

    public VideoFrameData Convert(AVFrame source, double pts, int fpsN, int fpsD)
    {
        ffmpeg.sws_scale(_ctx, source.data, source.linesize, 0, source.height, _dstData, _dstLinesize);
        var managed = new byte[_bufferSize];
        Marshal.Copy(_buffer, managed, 0, managed.Length);
        return new VideoFrameData(managed, _width, _height, _dstLinesize[0], pts, fpsN, fpsD);
    }

    public void Dispose()
    {
        if (_buffer != IntPtr.Zero) Marshal.FreeHGlobal(_buffer);
        if (_ctx != null) ffmpeg.sws_freeContext(_ctx);
    }
}
