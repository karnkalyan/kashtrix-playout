using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

public static class CgDemoFactory
{
    public static string GetCgDemoDirectory()
    {
        var candidates = new List<string?>
        {
            @"c:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H\cg-demo",
            Environment.GetEnvironmentVariable("KASHTRIX_WORKSPACE_ROOT") != null ? Path.Combine(Environment.GetEnvironmentVariable("KASHTRIX_WORKSPACE_ROOT")!, "cg-demo") : null,
            Path.Combine(Directory.GetCurrentDirectory(), "cg-demo"),
            Path.Combine(AppContext.BaseDirectory, "cg-demo")
        };
        foreach (var c in candidates)
        {
            if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c)) return c;
        }
        return @"c:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H\cg-demo";
    }

    public static List<CgProject> CreateDefaults(bool forceRefresh = false)
    {
        var dir = GetCgDemoDirectory();
        var loaded = new List<CgProject>();
        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var candidateDirs = new List<string>();
        var rootDemo = @"c:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H\cg-demo";
        if (Directory.Exists(rootDemo)) candidateDirs.Add(rootDemo);
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) && !candidateDirs.Contains(dir, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(dir);
        var curCgDemo = Path.Combine(Directory.GetCurrentDirectory(), "cg-demo");
        if (Directory.Exists(curCgDemo) && !candidateDirs.Contains(curCgDemo, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(curCgDemo);
        var baseCgDemo = Path.Combine(AppContext.BaseDirectory, "cg-demo");
        if (Directory.Exists(baseCgDemo) && !candidateDirs.Contains(baseCgDemo, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(baseCgDemo);

        foreach (var d in candidateDirs)
        {
            if (!Directory.Exists(d)) continue;
            var kcgFiles = Directory.GetFiles(d, "*.kcg");
            foreach (var file in kcgFiles.OrderBy(f => Path.GetFileName(f)))
            {
                try
                {
                    var proj = KashtrixCgFileService.LoadComposition(file);
                    if (proj != null && !string.IsNullOrWhiteSpace(proj.Name) && loadedNames.Add(proj.Name))
                    {
                        CgProjectSanitizer.Sanitize(proj);
                        loaded.Add(proj);
                    }
                }
                catch { }
            }
        }

        return loaded;
    }

    public static CgLayer CloneLayer(CgLayer l)
    {
        try
        {
            var json = JsonSerializer.Serialize(l);
            return JsonSerializer.Deserialize<CgLayer>(json) ?? l;
        }
        catch
        {
            return l;
        }
    }

    public static CgProject CloneProject(CgProject p)
    {
        try
        {
            var json = JsonSerializer.Serialize(p);
            return JsonSerializer.Deserialize<CgProject>(json) ?? p;
        }
        catch
        {
            return p;
        }
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars).Trim();
    }

    private static CgProject? MusicNowPlaying() => CreateDefaults().FirstOrDefault(x => x.Name.Contains("Music · Now Playing", StringComparison.OrdinalIgnoreCase));
    private static CgProject? MusicComingUp() => CreateDefaults().FirstOrDefault(x => x.Name.Contains("Music · Coming Up", StringComparison.OrdinalIgnoreCase));
    private static CgProject? NewsNowPlaying() => CreateDefaults().FirstOrDefault(x => x.Name.Contains("Now Playing", StringComparison.OrdinalIgnoreCase));
    private static CgProject? NewsComingUp() => CreateDefaults().FirstOrDefault(x => x.Name.Contains("Coming Up", StringComparison.OrdinalIgnoreCase));
    // SqueezeHorizontalMode="Left"
}
