using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace BroadcastPlayout.Ffmpeg;

public static unsafe class FfmpegRuntime
{
    private static bool _initialized;
    public static bool IsInitialized => _initialized;
    public static string Version { get; private set; } = "Not loaded";

    public static void Initialize(string rootPath)
    {
        if (_initialized) return;
        var required = new[] { "avcodec-63.dll", "avformat-63.dll", "avutil-61.dll", "avdevice-63.dll", "swscale-10.dll", "swresample-7.dll" };

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(rootPath)) candidates.Add(rootPath);
        try
        {
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "native", "ffmpeg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "..", "native", "ffmpeg"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "..", "BroadcastPlayout.App", "native", "ffmpeg"));
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KashtrixPlayout", "native", "ffmpeg"));
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 4 && dir != null; i++, dir = dir.Parent)
            {
                candidates.Add(Path.Combine(dir.FullName, "native", "ffmpeg"));
                candidates.Add(Path.Combine(dir.FullName, "src", "BroadcastPlayout.App", "native", "ffmpeg"));
            }
        }
        catch { }

        string? validPath = null;
        foreach (var c in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(c) && required.All(dll => File.Exists(Path.Combine(c, dll))))
            {
                validPath = c;
                break;
            }
        }

        if (validPath == null)
            throw new FileNotFoundException("Broadcast media decoding engine standby. Native media runtime components are not configured.");

        DynamicallyLoadedBindings.LibrariesPath = validPath;
        DynamicallyLoadedBindings.Initialize();
        // Device demuxers (Windows capture cards/webcams and desktop capture) are
        // registered explicitly. This is required by some shared Windows builds.
        try
        {
            ffmpeg.avdevice_register_all();
        }
        catch
        {
            // avdevice_register_all may throw NotSupportedException in dynamically loaded bindings if stubbed
        }
        try
        {
            ffmpeg.avformat_network_init();
        }
        catch
        {
            // avformat_network_init safe guard
        }
        Version = ffmpeg.av_version_info() ?? "unknown";
        _initialized = true;
    }

    public static void EnsureReady()
    {
        if (!_initialized) Initialize(Path.Combine(AppContext.BaseDirectory, "native", "ffmpeg"));
    }
}

public static unsafe class FfmpegError
{
    public static string ToFfmpegError(this int error)
    {
        if (error >= 0) return "OK";
        const int size = 1024;
        var buffer = stackalloc byte[size];
        ffmpeg.av_strerror(error, buffer, (ulong)size);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"FFmpeg error {error}";
    }

    public static int ThrowIfError(this int error)
    {
        if (error >= 0) return error;
        throw new InvalidOperationException(error.ToFfmpegError());
    }
}
