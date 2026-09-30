namespace BroadcastPlayout.Models;

/// <summary>
/// FIX42 professional transmission/control settings. New network transmitters, GPIO and HA takeover
/// remain OFF until explicitly enabled; OutputArmed defaults on to preserve existing NDI/DeckLink behavior.
/// </summary>
public sealed class ProfessionalBroadcastSettings
{
    public bool OutputArmed { get; set; } = true;

    // SMPTE ST 2110 / ST 2022-7
    public bool EnableSt2110 { get; set; }
    public bool EnableSt2022_7 { get; set; }
    public string St2110VideoDestinationA { get; set; } = "239.100.20.1";
    public string St2110VideoDestinationB { get; set; } = "239.100.20.2";
    public int St2110VideoPort { get; set; } = 50020;
    public string St2110AudioDestinationA { get; set; } = "239.100.30.1";
    public string St2110AudioDestinationB { get; set; } = "239.100.30.2";
    public int St2110AudioPort { get; set; } = 50030;
    public string St2110AncDestinationA { get; set; } = "239.100.40.1";
    public string St2110AncDestinationB { get; set; } = "239.100.40.2";
    public int St2110AncPort { get; set; } = 50040;
    public int St2110Mtu { get; set; } = 1400;
    public int St2110VideoPayloadType { get; set; } = 96;
    public int St2110AudioPayloadType { get; set; } = 97;
    public int St2110AncPayloadType { get; set; } = 98;

    // PTP monitor / clock source. This package monitors PTP and timestamps RTP from the
    // disciplined local clock; it does not alter the Windows system clock itself.
    public bool EnablePtpMonitor { get; set; }
    public string PtpDomain { get; set; } = "0";
    public int PtpEventPort { get; set; } = 319;
    public int PtpGeneralPort { get; set; } = 320;

    // AMWA NMOS sender control.
    public bool EnableNmos { get; set; }
    public int NmosHttpPort { get; set; } = 3210;
    public string NmosRegistrationUrl { get; set; } = string.Empty;

    // SCTE / TS / ANC.
    public bool EnableScte35Ts { get; set; }
    public string Scte35TsDestination { get; set; } = "239.100.35.1";
    public int Scte35TsPort { get; set; } = 50350;
    public int Scte35Pid { get; set; } = 0x1F00;
    public int Scte35PmtPid { get; set; } = 0x1000;
    public int Scte35ProgramNumber { get; set; } = 1;
    public bool EnableScte104Anc { get; set; } = true;
    public int Scte104VancLine { get; set; } = 11;

    // Captions / subtitles.
    public bool EnableCea608 { get; set; } = true;
    public bool EnableCea708 { get; set; } = true;
    public int CaptionVancLine { get; set; } = 9;
    public bool EnableDvbSubtitles { get; set; }
    public int DvbSubtitlePid { get; set; } = 0x1200;

    // GPIO / serial / VDCP.
    public bool EnableGpio { get; set; }
    public string GpioComPort { get; set; } = "COM3";
    public bool EnableRs422 { get; set; }
    public string Rs422ComPort { get; set; } = "COM4";
    public int Rs422Baud { get; set; } = 38400;
    public bool EnableVdcp { get; set; }

    // SNMP v2c monitoring/notifications.
    public bool EnableSnmp { get; set; }
    public int SnmpAgentPort { get; set; } = 1161;
    public string SnmpReadCommunity { get; set; } = "public";
    public string SnmpTrapHost { get; set; } = "127.0.0.1";
    public int SnmpTrapPort { get; set; } = 162;
    public string SnmpTrapCommunity { get; set; } = "public";

    // Dolby integration is intentionally adapter/licence based. Kashtrix never pretends to
    // provide an encoder when a licensed SDK/adapter has not been installed.
    public string DolbyMode { get; set; } = "Passthrough"; // Off | Passthrough | LicensedAdapter
    public string DolbyAdapterPath { get; set; } = string.Empty;

    // 1+1 / N+M high availability.
    public string HaMode { get; set; } = "Standalone"; // Standalone | Primary | Backup | Pool
    public string HaNodeId { get; set; } = Environment.MachineName;
    public int HaPriority { get; set; } = 100;
    public string HaMulticastAddress { get; set; } = "239.255.42.42";
    public int HaHeartbeatPort { get; set; } = 42420;
    public int HaControlPort { get; set; } = 42421;
    public int HaHeartbeatMilliseconds { get; set; } = 500;
    public int HaFailoverMilliseconds { get; set; } = 1800;
    public int HaCoordinatorTimeoutMilliseconds { get; set; } = 2500;
    public bool HaUseCoordinator { get; set; } = true;
    public int HaCapacity { get; set; } = 1;
}
