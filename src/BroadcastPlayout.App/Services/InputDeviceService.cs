using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BroadcastPlayout.Services;

public sealed record CaptureDeviceLists(IReadOnlyList<string> VideoDevices, IReadOnlyList<string> AudioDevices);

public static partial class InputDeviceService
{
    public static async Task<CaptureDeviceLists> EnumerateDirectShowAsync()
    {
        var ffmpegPath = FindFfmpegExecutable();
        if (ffmpegPath is null)
            return new CaptureDeviceLists([], []);

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = "-hide_banner -list_devices true -f dshow -i dummy",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null) return new CaptureDeviceLists([], []);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
        }

        var video = new List<string>();
        var audio = new List<string>();
        foreach (var line in stderr.Split('\n'))
        {
            if (line.Contains("Alternative name", StringComparison.OrdinalIgnoreCase)) continue;
            var match = DeviceRegex().Match(line);
            if (!match.Success) continue;
            var name = match.Groups["name"].Value.Trim();
            var kind = match.Groups["kind"].Value;
            if (kind.Equals("video", StringComparison.OrdinalIgnoreCase) && !video.Contains(name, StringComparer.OrdinalIgnoreCase)) video.Add(name);
            if (kind.Equals("audio", StringComparison.OrdinalIgnoreCase) && !audio.Contains(name, StringComparer.OrdinalIgnoreCase)) audio.Add(name);
        }

        return new CaptureDeviceLists(video, audio);
    }

    /// <summary>Finds the bundled FFmpeg executable from any standalone Kashtrix app.</summary>
    public static string? FindFfmpegExecutable()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "native", "ffmpeg", "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "native", "ffmpeg", "ffmpeg.exe"),
            Path.Combine(Environment.CurrentDirectory, "src", "BroadcastPlayout.App", "native", "ffmpeg", "ffmpeg.exe")
        };

        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; cursor is not null && depth < 9; depth++, cursor = cursor.Parent)
        {
            candidates.Add(Path.Combine(cursor.FullName, "src", "BroadcastPlayout.App", "native", "ffmpeg", "ffmpeg.exe"));
            candidates.Add(Path.Combine(cursor.FullName, "Kashtrix.Playout", "native", "ffmpeg", "ffmpeg.exe"));
            candidates.Add(Path.Combine(cursor.FullName, "native", "ffmpeg", "ffmpeg.exe"));
        }

        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    [GeneratedRegex("\\\"(?<name>[^\\\"]+)\\\"\\s+\\((?<kind>video|audio)\\)", RegexOptions.IgnoreCase)]
    private static partial Regex DeviceRegex();
}
