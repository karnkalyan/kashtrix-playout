using System.Text.Json;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

/// <summary>One CG composition currently live on a channel's PROGRAM bus.</summary>
public sealed class CgOnAirItem
{
    public string ChannelId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    /// <summary>Overlay/keyer layer. -1 = no routed layer.</summary>
    public int Layer { get; set; } = -1;
    /// <summary>CONTROLLER (CG Controller / API stack), PLAYOUT (quick CG / auto CG) or LOGO.</summary>
    public string Source { get; set; } = "CONTROLLER";
    public bool IsStopping { get; set; }
    public DateTime StartedUtc { get; set; }
    public double ElapsedSeconds { get; set; }
    public double DurationSeconds { get; set; }
    public bool Loop { get; set; }
    public int LayerCount { get; set; }
}

public sealed class CgOnAirSnapshot
{
    public string ChannelId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public List<CgOnAirItem> Items { get; set; } = [];
}

/// <summary>
/// File-backed, same-workstation registry of PROGRAM CG items. Playout writes one snapshot
/// per channel on its telemetry tick; CG Controller (desktop) and the API Gateway (web) read it.
/// Snapshots older than <see cref="StaleAfter"/> are treated as offline and ignored.
/// </summary>
public static class CgOnAirRegistry
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false, PropertyNameCaseInsensitive = true };
    public static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(15);

    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KashtrixPlayout", "LocalCg", "_onair");

    private static string FileFor(string channelId)
    {
        var safe = string.Concat((channelId ?? string.Empty).Trim().ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
        if (string.IsNullOrWhiteSpace(safe)) safe = "DEFAULT";
        return Path.Combine(Root, safe + ".json");
    }

    public static CgOnAirItem FromProject(string channelId, CgProject project, string source)
    {
        var started = project.StartedUtc == default ? DateTime.UtcNow : project.StartedUtc;
        return new CgOnAirItem
        {
            ChannelId = channelId,
            ProjectId = project.Id.ToString("D"),
            Name = project.Name,
            Category = project.Category ?? string.Empty,
            Layer = project.ExternalLayer,
            Source = source,
            IsStopping = project.IsStopping,
            StartedUtc = started,
            ElapsedSeconds = Math.Round(Math.Max(0, (DateTime.UtcNow - started).TotalSeconds), 2),
            DurationSeconds = project.DurationSeconds,
            Loop = project.Loop,
            LayerCount = project.Layers?.Count ?? 0
        };
    }

    public static void Publish(CgOnAirSnapshot snapshot)
    {
        try
        {
            Directory.CreateDirectory(Root);
            snapshot.UpdatedUtc = DateTime.UtcNow;
            var target = FileFor(snapshot.ChannelId);
            var temp = target + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, Json));
            File.Move(temp, target, true);
        }
        catch { /* telemetry must never break playout */ }
    }

    public static CgOnAirSnapshot? Read(string channelId)
    {
        try
        {
            var path = FileFor(channelId);
            if (!File.Exists(path)) return null;
            var snap = JsonSerializer.Deserialize<CgOnAirSnapshot>(File.ReadAllText(path), Json);
            if (snap is null || DateTime.UtcNow - snap.UpdatedUtc > StaleAfter) return null;
            return snap;
        }
        catch { return null; }
    }

    public static List<CgOnAirSnapshot> ReadAll()
    {
        var result = new List<CgOnAirSnapshot>();
        try
        {
            if (!Directory.Exists(Root)) return result;
            foreach (var path in Directory.EnumerateFiles(Root, "*.json"))
            {
                try
                {
                    var snap = JsonSerializer.Deserialize<CgOnAirSnapshot>(File.ReadAllText(path), Json);
                    if (snap is not null && DateTime.UtcNow - snap.UpdatedUtc <= StaleAfter) result.Add(snap);
                }
                catch { }
            }
        }
        catch { }
        return result;
    }
}
