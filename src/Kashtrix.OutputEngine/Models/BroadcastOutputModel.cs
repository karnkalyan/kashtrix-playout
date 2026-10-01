using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kashtrix.OutputEngine.Models;

public enum BroadcastOutputProtocol
{
    DeckLink,
    NDI,
    Matrox,
    AJA,
    DvbUdp,
    SRT,
    RTMP,
    HLS,
    MPD,
    RTSP
}

public enum OutputStatus
{
    Standby,
    Starting,
    Online,
    Warning,
    Error
}

public class OutputChannel : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    private string _name = "Output Channel";
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    private BroadcastOutputProtocol _protocol = BroadcastOutputProtocol.DeckLink;
    public BroadcastOutputProtocol Protocol
    {
        get => _protocol;
        set
        {
            if (SetField(ref _protocol, value))
            {
                OnPropertyChanged(nameof(ProtocolDisplayName));
                OnPropertyChanged(nameof(IsHardwareDevice));
                OnPropertyChanged(nameof(IsNetworkStream));
            }
        }
    }

    public string ProtocolDisplayName => Protocol switch
    {
        BroadcastOutputProtocol.DeckLink => "DECKLINK SDI / HDMI",
        BroadcastOutputProtocol.NDI => "NDI 6 / HX BROADCAST",
        BroadcastOutputProtocol.Matrox => "MATROX DSX / X.MIO",
        BroadcastOutputProtocol.AJA => "AJA KONA / CORVID",
        BroadcastOutputProtocol.DvbUdp => "DVB-TS UDP / RTP (CBR)",
        BroadcastOutputProtocol.SRT => "SRT (CALLER / LISTENER)",
        BroadcastOutputProtocol.RTMP => "RTMP / RTMPS LIVE",
        BroadcastOutputProtocol.HLS => "DVB-COMPLIANT HLS",
        BroadcastOutputProtocol.MPD => "DASH (MPD) STREAM",
        BroadcastOutputProtocol.RTSP => "RTSP STREAM",
        _ => Protocol.ToString()
    };

    public bool IsHardwareDevice => Protocol is BroadcastOutputProtocol.DeckLink or BroadcastOutputProtocol.Matrox or BroadcastOutputProtocol.AJA or BroadcastOutputProtocol.NDI;
    public bool IsNetworkStream => !IsHardwareDevice;

    private string _destinationUri = "DeckLink 8K Pro (Device 1 - SDI 1)";
    public string DestinationUri
    {
        get => _destinationUri;
        set => SetField(ref _destinationUri, value);
    }

    private string _rasterFormat = "1080p50";
    public string RasterFormat
    {
        get => _rasterFormat;
        set => SetField(ref _rasterFormat, value);
    }

    private double _targetFps = 50.0;
    public double TargetFps
    {
        get => _targetFps;
        set => SetField(ref _targetFps, value);
    }

    private double _runningFps = 50.0;
    public double RunningFps
    {
        get => _runningFps;
        set => SetField(ref _runningFps, value);
    }

    private long _framesTransmitted;
    public long FramesTransmitted
    {
        get => _framesTransmitted;
        set => SetField(ref _framesTransmitted, value);
    }

    private long _droppedFrames;
    public long DroppedFrames
    {
        get => _droppedFrames;
        set => SetField(ref _droppedFrames, value);
    }

    private double _bitrateMbps = 15.0;
    public double BitrateMbps
    {
        get => _bitrateMbps;
        set => SetField(ref _bitrateMbps, value);
    }

    private double _pcrJitterNs = 12.4;
    public double PcrJitterNs
    {
        get => _pcrJitterNs;
        set => SetField(ref _pcrJitterNs, value);
    }

    private double _latencyMs = 3.2;
    public double LatencyMs
    {
        get => _latencyMs;
        set => SetField(ref _latencyMs, value);
    }

    private int _bufferPercent = 95;
    public int BufferPercent
    {
        get => _bufferPercent;
        set => SetField(ref _bufferPercent, value);
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    private OutputStatus _status = OutputStatus.Online;
    public OutputStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusColorBrush));
                OnPropertyChanged(nameof(HasAlert));
                OnPropertyChanged(nameof(StatusBadgeText));
            }
        }
    }

    public string StatusBadgeText => Status switch
    {
        OutputStatus.Online => "ONLINE",
        OutputStatus.Starting => "STARTING",
        OutputStatus.Warning => "ALERT (!)",
        OutputStatus.Error => "FAULT (!)",
        _ => "STANDBY"
    };

    public string StatusColorBrush => Status switch
    {
        OutputStatus.Online => "#FF10B981", // Green
        OutputStatus.Starting => "#FF06B6D4", // Cyan
        OutputStatus.Warning => "#FFF59E0B", // Amber / Alert
        OutputStatus.Error => "#FFEF4444", // Red Fault
        _ => "#FF64748B" // Slate / Standby
    };

    private string _alertMessage = string.Empty;
    public string AlertMessage
    {
        get => _alertMessage;
        set
        {
            if (SetField(ref _alertMessage, value))
            {
                OnPropertyChanged(nameof(HasAlert));
            }
        }
    }

    public bool HasAlert => !string.IsNullOrWhiteSpace(AlertMessage) || Status is OutputStatus.Warning or OutputStatus.Error;

    // Broadcast DVB & Signaling properties
    public bool DvbStandardEnabled { get; set; } = true;
    public bool Scte35Enabled { get; set; } = true;
    public bool MosEnabled { get; set; } = true;
    public int ServiceId { get; set; } = 101;
    public int PmtPid { get; set; } = 256;
    public int VideoPid { get; set; } = 257;
    public int AudioPid { get; set; } = 258;
    public int PcrPid { get; set; } = 257;
    public int Scte35Pid { get; set; } = 500;
    public string ProviderName { get; set; } = "Kashtrix Broadcast";
    public string ServiceName { get; set; } = "Prime HD Playout";

    // GPU-accelerated encoding selection
    private string _gpuEncoder = "NVENC H.264 (CUDA)";
    public string GpuEncoder
    {
        get => _gpuEncoder;
        set => SetField(ref _gpuEncoder, value);
    }

    // Custom streaming URL presets (for RTMP/social/CDN outputs)
    private string _streamPreset = string.Empty;
    public string StreamPreset
    {
        get => _streamPreset;
        set => SetField(ref _streamPreset, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public class BroadcastEventLog
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Category { get; set; } = "GENERAL"; // SCTE-35, MOS, DVB-TS, HARDWARE, ALERT
    public string ChannelName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "INFO"; // INFO, WARN, ERROR, SUCCESS

    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    public string LevelColorBrush => Level switch
    {
        "WARN" => "#FFF59E0B",
        "ERROR" => "#FFEF4444",
        "SUCCESS" => "#FF10B981",
        _ => "#FF94A3B8"
    };
}

public class Scte35SpliceEvent
{
    public uint EventId { get; set; }
    public string CommandType { get; set; } = "splice_insert"; // splice_insert, time_signal
    public double DurationSeconds { get; set; } = 30.0;
    public string PtsTimestampHex { get; set; } = "0x00000000";
    public string State { get; set; } = "Active"; // Scheduled, Active, Complete
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
