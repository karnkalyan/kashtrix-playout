using System.IO.MemoryMappedFiles;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Outputs;

/// <summary>
/// Reader for zero-latency, local inter-process shared memory video frames
/// published by Kashtrix Studio, Playout Program Confidence, or CG Program.
/// </summary>
public sealed class SharedVideoBusReader : IDisposable
{
    private readonly string _mapName;
    private MemoryMappedFile? _map;
    private MemoryMappedViewAccessor? _view;
    private long _sequence;
    private double _lastPts;
    private DateTime _lastFrameUtc = DateTime.UtcNow;

    public SharedVideoBusReader(string mapName)
    {
        _mapName = string.IsNullOrWhiteSpace(mapName) ? VirtualOutputBridge.StudioProgramVideoMapName : mapName;
    }

    public bool TryRead(out VideoFrameData frame)
    {
        frame = null!;
        try
        {
            if (_view is null)
            {
                try
                {
                    _map = MemoryMappedFile.OpenExisting(_mapName, MemoryMappedFileRights.Read);
                    _view = _map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                }
                catch
                {
                    return false;
                }
            }

            if (_view.ReadInt32(0) != 0x4B545856) return false;

            var width = _view.ReadInt32(8);
            var height = _view.ReadInt32(12);
            var stride = _view.ReadInt32(16);
            var fps = _view.ReadDouble(20);
            var sequence = _view.ReadInt64(28);
            var length = _view.ReadInt32(44);

            if (sequence == _sequence || width <= 0 || height <= 0 || stride < width * 4 || length <= 0 || length > stride * height)
                return false;

            var pixels = new byte[length];
            _view.ReadArray(256, pixels, 0, pixels.Length);

            // Verify sequence was not overwritten during read (torn frame protection)
            if (_view.ReadInt64(28) != sequence) return false;

            var clampedFps = Math.Clamp(fps > 0 ? fps : 50.0, 1.0, 120.0);
            var denominator = 1000;
            var numerator = Math.Max(1, (int)Math.Round(clampedFps * denominator));

            var now = DateTime.UtcNow;
            var dt = (now - _lastFrameUtc).TotalSeconds;
            _lastFrameUtc = now;
            _lastPts += Math.Clamp(dt, 0.005, 0.1);

            frame = new VideoFrameData(pixels, width, height, stride, _lastPts, numerator, denominator);
            _sequence = sequence;
            return true;
        }
        catch
        {
            _view?.Dispose();
            _view = null;
            _map?.Dispose();
            _map = null;
            return false;
        }
    }

    public void Dispose()
    {
        _view?.Dispose();
        _view = null;
        _map?.Dispose();
        _map = null;
    }
}

/// <summary>
/// Reader for zero-latency, local inter-process shared memory audio chunks
/// published by Kashtrix Studio or Playout Program Confidence.
/// </summary>
public sealed class SharedAudioBusReader : IDisposable
{
    private readonly string _mapName;
    private MemoryMappedFile? _map;
    private MemoryMappedViewAccessor? _view;
    private long _sequence;
    private double _lastPts;

    public SharedAudioBusReader(string mapName)
    {
        _mapName = string.IsNullOrWhiteSpace(mapName) ? VirtualOutputBridge.StudioProgramAudioMapName : mapName;
    }

    public bool TryRead(out AudioChunk chunk)
    {
        chunk = null!;
        try
        {
            if (_view is null)
            {
                try
                {
                    _map = MemoryMappedFile.OpenExisting(_mapName, MemoryMappedFileRights.Read);
                    _view = _map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                }
                catch
                {
                    return false;
                }
            }

            if (_view.ReadInt32(0) != 0x4B545841) return false;

            var sampleRate = _view.ReadInt32(8);
            var channels = _view.ReadInt32(12);
            var bits = _view.ReadInt32(16);
            var sequence = _view.ReadInt64(20);
            var byteCount = _view.ReadInt32(36);

            if (sequence == _sequence || byteCount <= 0 || byteCount > 384000)
                return false;

            var pcm = new byte[byteCount];
            _view.ReadArray(256, pcm, 0, pcm.Length);

            if (_view.ReadInt64(20) != sequence) return false;

            var effectiveRate = sampleRate > 0 ? sampleRate : 48000;
            var effectiveChannels = channels > 0 ? channels : 2;
            var effectiveBits = bits > 0 ? bits : 16;
            var chunkSeconds = (double)byteCount / (effectiveRate * effectiveChannels * (effectiveBits / 8));
            _lastPts += chunkSeconds;

            chunk = new AudioChunk(pcm, _lastPts, effectiveRate, effectiveChannels, effectiveBits);
            _sequence = sequence;
            return true;
        }
        catch
        {
            _view?.Dispose();
            _view = null;
            _map?.Dispose();
            _map = null;
            return false;
        }
    }

    public void Dispose()
    {
        _view?.Dispose();
        _view = null;
        _map?.Dispose();
        _map = null;
    }
}
