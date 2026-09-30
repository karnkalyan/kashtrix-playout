using System.Net;
using System.Net.Sockets;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Professional;

/// <summary>Builds SCTE-35 splice_insert() sections and a compact MPEG-TS sidecar with PMT stream_type 0x86.</summary>
public static class Scte35Codec
{
    public static byte[] BuildSpliceInsert(uint eventId, string spliceType, TimeSpan? breakDuration = null, long pts90k = 0,
        bool autoReturn = true, ushort uniqueProgramId = 1, byte availNum = 0, byte availsExpected = 0)
    {
        var type = (spliceType ?? "Start Normal").Trim().ToUpperInvariant();
        var cancel = type.Contains("CANCEL", StringComparison.Ordinal);
        var outOfNetwork = type.StartsWith("START", StringComparison.Ordinal);
        var immediate = type.Contains("IMMEDIATE", StringComparison.Ordinal);

        var command = new List<byte>();
        WriteUInt32(command, eventId);
        command.Add((byte)(cancel ? 0x80 : 0x00)); // splice_event_cancel_indicator + reserved bits
        if (!cancel)
        {
            var hasDuration = breakDuration.HasValue && breakDuration.Value > TimeSpan.Zero && outOfNetwork;
            var flags = 0x40; // program_splice_flag
            if (outOfNetwork) flags |= 0x80;
            if (hasDuration) flags |= 0x20;
            if (immediate) flags |= 0x10;
            command.Add((byte)flags);

            if (!immediate)
            {
                // splice_time(): time_specified_flag=1 + 33-bit PTS in the 90 kHz clock domain.
                var pts = Math.Max(0L, pts90k) & 0x1FFFFFFFFL;
                var ptsHighBit = (int)((pts >> 32) & 0x01L);
                command.Add((byte)(0xFE | ptsHighBit));
                command.Add((byte)(pts >> 24)); command.Add((byte)(pts >> 16)); command.Add((byte)(pts >> 8)); command.Add((byte)pts);
            }
            if (hasDuration)
            {
                var dur = Math.Max(0L, (long)Math.Round(breakDuration!.Value.TotalSeconds * 90000d)) & 0x1FFFFFFFFL;
                var durationHighBit = (int)((dur >> 32) & 0x01L);
                command.Add((byte)((autoReturn ? 0x80 : 0x00) | 0x7E | durationHighBit));
                command.Add((byte)(dur >> 24)); command.Add((byte)(dur >> 16)); command.Add((byte)(dur >> 8)); command.Add((byte)dur);
            }
            command.Add((byte)(uniqueProgramId >> 8)); command.Add((byte)uniqueProgramId);
            command.Add(availNum); command.Add(availsExpected);
        }

        var section = new List<byte> { 0xFC, 0x30, 0x00, 0x00 };
        section.AddRange([0x00, 0x00, 0x00, 0x00, 0x00]); // encryption/pts_adjustment
        section.Add(0x00); // cw_index
        var cmdLen = command.Count;
        section.Add(0xFF); // tier high bits
        section.Add((byte)(0xF0 | ((cmdLen >> 8) & 0x0F)));
        section.Add((byte)(cmdLen & 0xFF));
        section.Add(0x05); // splice_insert
        section.AddRange(command);
        section.Add(0x00); section.Add(0x00); // descriptor_loop_length
        var sectionLength = section.Count - 3 + 4;
        section[1] = (byte)(0x30 | ((sectionLength >> 8) & 0x0F)); section[2] = (byte)sectionLength;
        WriteUInt32(section, MpegCrc32(section.ToArray()));
        return section.ToArray();
    }

    public static uint MpegCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data) { crc ^= (uint)b << 24; for (var i = 0; i < 8; i++) crc = (crc & 0x80000000) != 0 ? (crc << 1) ^ 0x04C11DB7 : crc << 1; }
        return crc;
    }
    private static void WriteUInt32(List<byte> b, uint v) { b.Add((byte)(v >> 24)); b.Add((byte)(v >> 16)); b.Add((byte)(v >> 8)); b.Add((byte)v); }
}

public sealed class Scte35TsSender : IDisposable
{
    private readonly UdpClient _udp = new(); private int _ccPat, _ccPmt, _ccScte;
    public string LastError { get; private set; } = string.Empty;

    public void SendCue(ProfessionalBroadcastSettings s, PlaylistItem item, uint eventId, long currentPts90k)
    {
        if (!s.EnableScte35Ts) return;
        try
        {
            var ep = new IPEndPoint(IPAddress.Parse(s.Scte35TsDestination), Math.Clamp(s.Scte35TsPort, 1, 65535));
            foreach (var packet in PacketizePsi(BuildPat((ushort)s.Scte35ProgramNumber, (ushort)s.Scte35PmtPid), 0x0000, ref _ccPat)) _udp.Send(packet, packet.Length, ep);
            foreach (var packet in PacketizePsi(BuildPmt((ushort)s.Scte35ProgramNumber, (ushort)s.Scte35Pid), (ushort)s.Scte35PmtPid, ref _ccPmt)) _udp.Send(packet, packet.Length, ep);
            var scheduledPts = item.SctePts90k > 0 ? item.SctePts90k : currentPts90k + (long)item.SctePreRollMilliseconds * 90L;
            TimeSpan? duration = item.ScteDurationSeconds > 0 ? TimeSpan.FromSeconds(item.ScteDurationSeconds) : null;
            var section = Scte35Codec.BuildSpliceInsert(eventId, item.ScteSpliceType, duration, scheduledPts, item.ScteAutoReturn,
                (ushort)item.ScteUniqueProgramId, (byte)item.ScteAvailNum, (byte)item.ScteAvailsExpected);
            foreach (var packet in PacketizePsi(section, (ushort)s.Scte35Pid, ref _ccScte)) _udp.Send(packet, packet.Length, ep);
            LastError = string.Empty;
        }
        catch (Exception ex) { LastError = ex.Message; }
    }

    private static byte[] BuildPat(ushort program, ushort pmtPid)
    {
        var b = new List<byte> { 0x00,0xB0,0x0D,0x00,0x01,0xC1,0x00,0x00,(byte)(program>>8),(byte)program,(byte)(0xE0 | ((pmtPid>>8)&0x1F)),(byte)pmtPid };
        var crc = Scte35Codec.MpegCrc32(b.ToArray()); b.Add((byte)(crc >> 24)); b.Add((byte)(crc >> 16)); b.Add((byte)(crc >> 8)); b.Add((byte)crc); return b.ToArray();
    }
    private static byte[] BuildPmt(ushort program, ushort sctePid)
    {
        var b = new List<byte> { 0x02,0xB0,0x12,(byte)(program>>8),(byte)program,0xC1,0x00,0x00,
            (byte)(0xE0|((sctePid>>8)&0x1F)),(byte)sctePid,0xF0,0x00, 0x86,(byte)(0xE0|((sctePid>>8)&0x1F)),(byte)sctePid,0xF0,0x00 };
        var crc = Scte35Codec.MpegCrc32(b.ToArray()); b.Add((byte)(crc >> 24)); b.Add((byte)(crc >> 16)); b.Add((byte)(crc >> 8)); b.Add((byte)crc); return b.ToArray();
    }
    private static IReadOnlyList<byte[]> PacketizePsi(byte[] section, ushort pid, ref int cc)
    {
        var packets = new List<byte[]>(); var offset = 0; var first = true;
        while (offset < section.Length)
        {
            var packet = Enumerable.Repeat((byte)0xFF, 188).ToArray(); packet[0] = 0x47; packet[1] = (byte)((first ? 0x40 : 0) | ((pid >> 8) & 0x1F)); packet[2] = (byte)pid; packet[3] = (byte)(0x10 | (cc++ & 0x0F));
            var p = 4; if (first) packet[p++] = 0x00; var n = Math.Min(188 - p, section.Length - offset); Buffer.BlockCopy(section, offset, packet, p, n); offset += n; first = false; packets.Add(packet);
        }
        return packets;
    }
    public void Dispose() => _udp.Dispose();
}
