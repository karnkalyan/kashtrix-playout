using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using Kashtrix.OutputTypes.Models;

namespace Kashtrix.OutputTypes.Services;

public class BroadcastOutputEngine
{
    private static readonly Lazy<BroadcastOutputEngine> _instance = new(() => new BroadcastOutputEngine());
    public static BroadcastOutputEngine Instance => _instance.Value;

    public ObservableCollection<OutputChannel> Outputs { get; } = [];

    private readonly DispatcherTimer _cadenceTimer;
    private readonly Random _rand = new();
    private long _cycleCount = 0;

    public BroadcastOutputEngine()
    {
        InitializeDefaultOutputs();

        _cadenceTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _cadenceTimer.Tick += OnCadenceTick;
        _cadenceTimer.Start();
    }

    private void InitializeDefaultOutputs()
    {
        Outputs.Add(new OutputChannel
        {
            Name = "DeckLink 8K Pro (SDI 1 - Main TX)",
            Protocol = BroadcastOutputProtocol.DeckLink,
            DestinationUri = "Blackmagic DeckLink 8K Pro (Subdevice 1)",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 1500.0, // Uncompressed SDI 1.5G-SDI
            PcrJitterNs = 4.2,
            LatencyMs = 1.8,
            BufferPercent = 98,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceId = 101,
            PmtPid = 256,
            VideoPid = 257,
            AudioPid = 258,
            PcrPid = 257,
            Scte35Pid = 500,
            ServiceName = "Prime HD Master Playout"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "NDI 6 HX Studio Broadcast Feed",
            Protocol = BroadcastOutputProtocol.NDI,
            DestinationUri = "KASHTRIX-STUDIO (Main Program)",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 125.0,
            PcrJitterNs = 6.8,
            LatencyMs = 8.5,
            BufferPercent = 96,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Prime HD NDI Studio"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "Matrox DSX SDI Hardware Mux",
            Protocol = BroadcastOutputProtocol.Matrox,
            DestinationUri = "Matrox DSX.LE4 Card 0 (Port A)",
            RasterFormat = "1080i50",
            TargetFps = 25.0,
            RunningFps = 25.0,
            BitrateMbps = 1485.0,
            PcrJitterNs = 5.1,
            LatencyMs = 2.0,
            BufferPercent = 97,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Prime HD Matrox Master"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "AJA Kona 5 12G-SDI Auxiliary",
            Protocol = BroadcastOutputProtocol.AJA,
            DestinationUri = "AJA Kona 5 (Device 0 - Channel 1)",
            RasterFormat = "2160p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 11880.0, // 12G-SDI UHD
            PcrJitterNs = 3.9,
            LatencyMs = 1.5,
            BufferPercent = 99,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Space 4K UHD Master"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "DVB-TS UDP Primary Headend Multicast",
            Protocol = BroadcastOutputProtocol.DvbUdp,
            DestinationUri = "udp://239.192.10.101:5000?pkt_size=1316&ttl=32",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 12.5,
            PcrJitterNs = 14.2,
            LatencyMs = 18.0,
            BufferPercent = 94,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            DvbStandardEnabled = true,
            Scte35Enabled = true,
            ServiceId = 101,
            ServiceName = "Prime HD DVB Satellite Transport"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "SRT Cloud Distribution (Caller)",
            Protocol = BroadcastOutputProtocol.SRT,
            DestinationUri = "srt://ott-gateway.kashtrix.net:9000?mode=caller&latency=120&passphrase=KashtrixBroadcast2026",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 49.98,
            BitrateMbps = 8.5,
            PcrJitterNs = 16.5,
            LatencyMs = 122.4,
            BufferPercent = 91,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Prime HD Cloud SRT"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "RTMP Live Primary (YouTube / CDN)",
            Protocol = BroadcastOutputProtocol.RTMP,
            DestinationUri = "rtmp://a.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 6.5,
            PcrJitterNs = 22.0,
            LatencyMs = 850.0,
            BufferPercent = 93,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Prime HD Social RTMP"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "DVB-Compliant HLS OTT Stream",
            Protocol = BroadcastOutputProtocol.HLS,
            DestinationUri = "http://cdn-edge.kashtrix.net/live/primehd/index.m3u8",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 7.2,
            PcrJitterNs = 18.1,
            LatencyMs = 2100.0,
            BufferPercent = 95,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            ServiceName = "Prime HD OTT HLS"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "MPEG-DASH (MPD) Adaptive Live",
            Protocol = BroadcastOutputProtocol.MPD,
            DestinationUri = "http://cdn-edge.kashtrix.net/live/primehd/manifest.mpd",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 7.0,
            PcrJitterNs = 19.4,
            LatencyMs = 2200.0,
            BufferPercent = 95,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            ServiceName = "Prime HD DASH MPD"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "YouTube Live (RTMP)",
            Protocol = BroadcastOutputProtocol.RTMP,
            DestinationUri = "rtmp://a.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx-xxxx",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 6.0,
            PcrJitterNs = 20.1,
            LatencyMs = 1200.0,
            BufferPercent = 94,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            StreamPreset = "YouTube Live",
            ServiceName = "Prime HD YouTube"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "Facebook Live (RTMPS)",
            Protocol = BroadcastOutputProtocol.RTMP,
            DestinationUri = "rtmps://live-api-s.facebook.com:443/rtmp/FB-xxxx-xxxx-xxxx",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 4.5,
            PcrJitterNs = 24.3,
            LatencyMs = 1500.0,
            BufferPercent = 92,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            StreamPreset = "Facebook Live",
            ServiceName = "Prime HD Facebook"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "Twitch Live (RTMP)",
            Protocol = BroadcastOutputProtocol.RTMP,
            DestinationUri = "rtmp://live.twitch.tv/app/live_xxxxxxxx_xxxxxxxxxx",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 6.0,
            PcrJitterNs = 18.9,
            LatencyMs = 900.0,
            BufferPercent = 95,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            StreamPreset = "Twitch Live",
            ServiceName = "Prime HD Twitch"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "TikTok Live (RTMP)",
            Protocol = BroadcastOutputProtocol.RTMP,
            DestinationUri = "rtmp://push-rtmp-f5-tt.tiktokcdn.com/stage/stream-xxxxxxxxxxxx",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 4.0,
            PcrJitterNs = 26.1,
            LatencyMs = 1800.0,
            BufferPercent = 91,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            StreamPreset = "TikTok Live",
            ServiceName = "Prime HD TikTok"
        });

        Outputs.Add(new OutputChannel
        {
            Name = "DVB-TS UDP Unicast (IP Transport)",
            Protocol = BroadcastOutputProtocol.DvbUdp,
            DestinationUri = "udp://192.168.1.100:5000?pkt_size=1316&bitrate=15000000",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 50.0,
            BitrateMbps = 15.0,
            PcrJitterNs = 11.2,
            LatencyMs = 12.0,
            BufferPercent = 96,
            Status = OutputStatus.Standby,
            IsEnabled = false,
            DvbStandardEnabled = true,
            Scte35Enabled = true,
            GpuEncoder = "NVENC H.265/HEVC (CUDA)",
            ServiceId = 102,
            ServiceName = "Prime HD IP Unicast"
        });

        // Add 1 test channel with Alert status to demonstrate real-time "!" alert monitoring
        Outputs.Add(new OutputChannel
        {
            Name = "Backup Headend UDP (Secondary Route)",
            Protocol = BroadcastOutputProtocol.DvbUdp,
            DestinationUri = "udp://239.192.10.102:5000",
            RasterFormat = "1080p50",
            TargetFps = 50.0,
            RunningFps = 48.2,
            FramesTransmitted = 34500,
            DroppedFrames = 12,
            BitrateMbps = 11.8,
            PcrJitterNs = 44.8,
            LatencyMs = 38.0,
            BufferPercent = 74,
            Status = OutputStatus.Standby,
            AlertMessage = "Minor PCR jitter variance (>40ns) detected on secondary interface; packets recovered via SMPTE 2022-1 FEC.",
            IsEnabled = false,
            GpuEncoder = "NVENC H.264 (CUDA)",
            ServiceName = "Prime HD Backup DVB"
        });
    }

    private void OnCadenceTick(object? sender, EventArgs e)
    {
        _cycleCount++;

        foreach (var ch in Outputs)
        {
            if (!ch.IsEnabled || ch.Status == OutputStatus.Standby) { ch.RunningFps = 0.0; ch.BitrateMbps = 0.0; continue; }

            // Increment frame counts based on target FPS (200ms tick = ~10 frames @ 50fps, ~5 frames @ 25fps)
            var framesThisTick = (long)Math.Round(ch.TargetFps * 0.2);
            ch.FramesTransmitted += framesThisTick;

            // Microscopic cadence variance for realism
            if (ch.Status != OutputStatus.Warning && ch.Status != OutputStatus.Error)
            {
                var jitter = (_rand.NextDouble() - 0.5) * 0.04;
                ch.RunningFps = Math.Round(ch.TargetFps + jitter, 2);
            }
            else
            {
                ch.RunningFps = Math.Round(ch.TargetFps - 1.8 + (_rand.NextDouble() * 0.4), 2);
            }
        }

        // Periodic DVB table logging every 25 cycles (5 seconds)
        if (_cycleCount % 25 == 0)
        {
            Scte35DvbService.Instance.LogDvbTableTransmission("PAT (Program Association Table)", 0x0000, (int)(_cycleCount / 25) % 32);
            Scte35DvbService.Instance.LogDvbTableTransmission("PMT (Program Map Table)", 0x0100, (int)(_cycleCount / 25) % 32);
            Scte35DvbService.Instance.LogDvbTableTransmission("SDT (Service Description Table)", 0x0011, (int)(_cycleCount / 25) % 32);
        }

        // Periodic MOS heartbeat every 50 cycles (10 seconds)
        if (_cycleCount % 50 == 0)
        {
            Scte35DvbService.Instance.Log("MOS", "[MOS HEARTBEAT] Active Newsroom Socket connected | Ping ACK <mosAck><objID>PLAYOUT_NODE_01</objID><status>ACK</status></mosAck>", "INFO");
        }
    }

    public void ToggleOutput(OutputChannel channel)
    {
        if (channel.IsEnabled)
        {
            channel.IsEnabled = false;
            channel.Status = OutputStatus.Standby;
            Scte35DvbService.Instance.Log("HARDWARE", $"Output '{channel.Name}' stopped by operator", "WARN");
        }
        else
        {
            channel.IsEnabled = true;
            channel.Status = OutputStatus.Online;
            channel.AlertMessage = string.Empty;
            Scte35DvbService.Instance.Log("HARDWARE", $"Output '{channel.Name}' started successfully on {channel.ProtocolDisplayName}", "SUCCESS");
        }
    }

    public void StartOutput(OutputChannel channel)
    {
        channel.IsEnabled = true;
        channel.Status = OutputStatus.Online;
        channel.AlertMessage = string.Empty;
        Scte35DvbService.Instance.Log("ENGINE", $"Started output '{channel.Name}'", "SUCCESS");
    }

    public void StopOutput(OutputChannel channel)
    {
        channel.IsEnabled = false;
        channel.Status = OutputStatus.Standby;
        Scte35DvbService.Instance.Log("ENGINE", $"Stopped output '{channel.Name}'", "WARN");
    }

    public void RestartOutput(OutputChannel channel)
    {
        channel.IsEnabled = true;
        channel.Status = OutputStatus.Starting;
        Scte35DvbService.Instance.Log("ENGINE", $"Restarting output '{channel.Name}'", "INFO");
        Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => StartOutput(channel)), DispatcherPriority.Background);
    }

    public void DeleteOutput(OutputChannel channel)
    {
        StopOutput(channel);
        Outputs.Remove(channel);
        Scte35DvbService.Instance.Log("CONFIG", $"Deleted output '{channel.Name}'", "WARN");
    }

    public void ResetCounters()
    {
        foreach (var ch in Outputs)
        {
            ch.FramesTransmitted = 0;
            ch.DroppedFrames = 0;
            if (ch.Status == OutputStatus.Warning)
            {
                ch.Status = OutputStatus.Online;
                ch.AlertMessage = string.Empty;
            }
        }
        Scte35DvbService.Instance.Log("GENERAL", "All output frame counters and error flags have been reset by operator", "INFO");
    }
}
