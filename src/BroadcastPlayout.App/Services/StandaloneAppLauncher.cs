using System.Diagnostics;
using System.Text.Json;

namespace BroadcastPlayout.Services;

public static class StandaloneAppLauncher
{
    private sealed record AppInfo(string ProjectName, string ExeName, string? DisplayName = null)
    {
        public string UserFacingName => string.IsNullOrWhiteSpace(DisplayName) ? ProjectName : DisplayName;
    }

    private static readonly Dictionary<string, AppInfo> Apps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CGEditor"] = new("Kashtrix.CGEditor", "Kashtrix.CGEditor.exe", "Kashtrix CG Designer"),
        ["Kashtrix.CGEditor"] = new("Kashtrix.CGEditor", "Kashtrix.CGEditor.exe", "Kashtrix CG Designer"),
        ["Scheduler"] = new("Kashtrix.Scheduler", "Kashtrix.Scheduler.exe", "Kashtrix Broadcast Scheduler"),
        ["Kashtrix.Scheduler"] = new("Kashtrix.Scheduler", "Kashtrix.Scheduler.exe", "Kashtrix Broadcast Scheduler"),
        ["PlaylistScheduler"] = new("Kashtrix.Scheduler", "Kashtrix.Scheduler.exe", "Kashtrix Broadcast Scheduler"),
        ["Kashtrix.PlaylistScheduler"] = new("Kashtrix.Scheduler", "Kashtrix.Scheduler.exe", "Kashtrix Broadcast Scheduler"),
        ["PlaylistEditor"] = new("Kashtrix.PlaylistEditor", "Kashtrix.PlaylistEditor.exe", "Kashtrix Playlist Editor"),
        ["Kashtrix.PlaylistEditor"] = new("Kashtrix.PlaylistEditor", "Kashtrix.PlaylistEditor.exe", "Kashtrix Playlist Editor"),
        ["Settings"] = new("Kashtrix.Settings", "Kashtrix.Settings.exe", "Kashtrix Settings"),
        ["Kashtrix.Settings"] = new("Kashtrix.Settings", "Kashtrix.Settings.exe", "Kashtrix Settings"),
        ["Multiview"] = new("Kashtrix.Multiview", "Kashtrix.Multiview.exe", "Kashtrix Multiviewer"),
        ["Kashtrix.Multiview"] = new("Kashtrix.Multiview", "Kashtrix.Multiview.exe", "Kashtrix Multiviewer"),
        ["Multiviewer"] = new("Kashtrix.Multiview", "Kashtrix.Multiview.exe", "Kashtrix Multiviewer"),
        ["Kashtrix.Multiviewer"] = new("Kashtrix.Multiview", "Kashtrix.Multiview.exe", "Kashtrix Multiviewer"),
        ["ChannelController"] = new("Kashtrix.ChannelController", "Kashtrix.ChannelController.exe", "Kashtrix Channel Controller"),
        ["Kashtrix.ChannelController"] = new("Kashtrix.ChannelController", "Kashtrix.ChannelController.exe", "Kashtrix Channel Controller"),
        ["CGController"] = new("Kashtrix.CGController", "Kashtrix.CGController.exe", "Kashtrix CG Controller"),
        ["Kashtrix.CGController"] = new("Kashtrix.CGController", "Kashtrix.CGController.exe", "Kashtrix CG Controller"),
        ["FileManager"] = new("Kashtrix.FileManager", "Kashtrix.FileManager.exe", "Kashtrix File Manager"),
        ["Kashtrix.FileManager"] = new("Kashtrix.FileManager", "Kashtrix.FileManager.exe", "Kashtrix File Manager"),
        ["QCController"] = new("Kashtrix.QCController", "Kashtrix.QCController.exe", "Kashtrix QC Controller"),
        ["Kashtrix.QCController"] = new("Kashtrix.QCController", "Kashtrix.QCController.exe", "Kashtrix QC Controller"),
        ["IngestServer"] = new("Kashtrix.IngestServer", "Kashtrix.IngestServer.exe", "Kashtrix Ingest Server"),
        ["Kashtrix.IngestServer"] = new("Kashtrix.IngestServer", "Kashtrix.IngestServer.exe", "Kashtrix Ingest Server"),
        ["Ingest"] = new("Kashtrix.IngestServer", "Kashtrix.IngestServer.exe", "Kashtrix Ingest Server"),
        ["Kashtrix.Ingest"] = new("Kashtrix.IngestServer", "Kashtrix.IngestServer.exe", "Kashtrix Ingest Server"),
        ["MAM"] = new("Kashtrix.MAM", "Kashtrix.MAM.exe", "Kashtrix Media Asset Management"),
        ["Kashtrix.MAM"] = new("Kashtrix.MAM", "Kashtrix.MAM.exe", "Kashtrix Media Asset Management"),
        ["NRCS"] = new("Kashtrix.NRCS", "Kashtrix.NRCS.exe", "Kashtrix NRCS Newsroom"),
        ["Kashtrix.NRCS"] = new("Kashtrix.NRCS", "Kashtrix.NRCS.exe", "Kashtrix NRCS Newsroom"),
        ["Prompter"] = new("Kashtrix.Prompter", "Kashtrix.Prompter.exe", "Kashtrix Teleprompter"),
        ["Kashtrix.Prompter"] = new("Kashtrix.Prompter", "Kashtrix.Prompter.exe", "Kashtrix Teleprompter"),
        ["HAController"] = new("Kashtrix.HAController", "Kashtrix.HAController.exe", "Kashtrix HA Controller"),
        ["Kashtrix.HAController"] = new("Kashtrix.HAController", "Kashtrix.HAController.exe", "Kashtrix HA Controller"),
        ["ApiGateway"] = new("Kashtrix.ApiGateway", "Kashtrix.ApiGateway.exe", "Kashtrix API Gateway"),
        ["Kashtrix.ApiGateway"] = new("Kashtrix.ApiGateway", "Kashtrix.ApiGateway.exe", "Kashtrix API Gateway"),
        ["API Gateway"] = new("Kashtrix.ApiGateway", "Kashtrix.ApiGateway.exe", "Kashtrix API Gateway"),
        ["Kashtrix.API Gateway"] = new("Kashtrix.ApiGateway", "Kashtrix.ApiGateway.exe", "Kashtrix API Gateway"),
        ["OutputEngine"] = new("Kashtrix.OutputEngine", "Kashtrix.OutputEngine.exe", "Kashtrix Output Engine"),
        ["Kashtrix.OutputEngine"] = new("Kashtrix.OutputEngine", "Kashtrix.OutputEngine.exe", "Kashtrix Output Engine"),
        ["InputEngine"] = new("Kashtrix.InputEngine", "Kashtrix.InputEngine.exe", "Kashtrix Input Engine"),
        ["Kashtrix.InputEngine"] = new("Kashtrix.InputEngine", "Kashtrix.InputEngine.exe", "Kashtrix Input Engine"),
        ["Playout"] = new("Kashtrix.Playout", "Kashtrix.Playout.exe", "Kashtrix Playout"),
        ["Playout.app"] = new("Kashtrix.Playout", "Kashtrix.Playout.exe", "Kashtrix Playout"),
        ["PlayoutEngine"] = new("Kashtrix.Playout", "Kashtrix.Playout.exe", "Kashtrix Playout"),
        ["Kashtrix.Playout"] = new("Kashtrix.Playout", "Kashtrix.Playout.exe", "Kashtrix Playout")
    };

    public static bool Launch(string appKey, string? arguments = null)
    {
        var app = FindApp(appKey);
        if (app is null)
            throw new ArgumentOutOfRangeException(nameof(appKey), appKey, "Unknown Kashtrix application.");

        var exe = ResolveExecutable(app);
        if (exe is null) return false;

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = false
        });
        return process is not null;
    }

    public static bool Launch(string appKey, out string error, string? arguments = null)
    {
        try
        {
            var app = FindApp(appKey);
            if (app is null)
            {
                error = $"Unknown Kashtrix application: {appKey}";
                return false;
            }

            var exe = ResolveExecutable(app);
            if (exe is null)
            {
                error = $"{app.UserFacingName} executable was not found. Please ensure the application is built.";
                return false;
            }

            if (!KashtrixLicenseAuthority.VerifyApplicationStartup(app.ProjectName, out var licenseError))
            {
                error = licenseError;
                return false;
            }

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = false
            });

            if (process is null)
            {
                error = $"Windows did not start {app.UserFacingName}.";
                return false;
            }

            // Detect immediate startup failures instead of making the launcher appear to do nothing.
            if (process.WaitForExit(900) && process.ExitCode != 0)
            {
                var log = StandaloneAppDiagnostics.LogPath(app.UserFacingName);
                error = $"{app.UserFacingName} exited during startup (exit code {process.ExitCode})." +
                        (File.Exists(log) ? $" Startup log: {log}" : string.Empty);
                return false;
            }

            AuditLogService.Write("APP_LAUNCH", app.UserFacingName, exe);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Unable to open {appKey}: {ex.Message}";
            return false;
        }
    }

    private static AppInfo? FindApp(string keyOrName)
    {
        if (string.IsNullOrWhiteSpace(keyOrName)) return null;
        if (Apps.TryGetValue(keyOrName, out var app)) return app;
        return Apps.Values.FirstOrDefault(x =>
            x.ProjectName.Equals(keyOrName, StringComparison.OrdinalIgnoreCase) ||
            x.ExeName.Equals(keyOrName, StringComparison.OrdinalIgnoreCase) ||
            x.UserFacingName.Equals(keyOrName, StringComparison.OrdinalIgnoreCase) ||
            (keyOrName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && x.ExeName.Equals(keyOrName, StringComparison.OrdinalIgnoreCase)) ||
            (keyOrName.StartsWith("Kashtrix.", StringComparison.OrdinalIgnoreCase) && x.ProjectName.Equals(keyOrName, StringComparison.OrdinalIgnoreCase)));
    }

    public static string? ResolveExecutable(string exeOrKey)
    {
        if (string.IsNullOrWhiteSpace(exeOrKey)) return null;
        var app = FindApp(exeOrKey);
        if (app is not null)
        {
            var res = ResolveExecutable(app);
            if (res is not null) return res;
        }
        if (File.Exists(exeOrKey)) return Path.GetFullPath(exeOrKey);
        return null;
    }

    private static string? ResolveExecutable(AppInfo app)
    {
        // 1) Installed layout: check root and subfolders under AppContext.BaseDirectory
        var local = Path.Combine(AppContext.BaseDirectory, app.ExeName);
        if (File.Exists(local)) return local;

        if (app.ProjectName.Equals("Kashtrix.Playout", StringComparison.OrdinalIgnoreCase))
        {
            var localPlayout = Path.Combine(AppContext.BaseDirectory, "Kashtrix.Playout.exe");
            if (File.Exists(localPlayout)) return localPlayout;
        }

        // Subdirectories in installation folder (e.g. C:\Program Files\Kashtrix\PlayoutSuite\CGEditor\Kashtrix.CGEditor.exe)
        var subDirsToTry = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            app.ProjectName,
            app.ProjectName.StartsWith("Kashtrix.", StringComparison.OrdinalIgnoreCase) ? app.ProjectName["Kashtrix.".Length..] : app.ProjectName,
            Path.GetFileNameWithoutExtension(app.ExeName),
            Path.GetFileNameWithoutExtension(app.ExeName).Replace("Kashtrix.", "")
        };

        foreach (var sub in subDirsToTry)
        {
            var subPath = Path.Combine(AppContext.BaseDirectory, sub, app.ExeName);
            if (File.Exists(subPath)) return subPath;
        }

        // Also check standard Program Files installation paths
        var pfList = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Kashtrix", "PlayoutSuite"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Kashtrix", "PlayoutSuite")
        };
        foreach (var pf in pfList)
        {
            if (!Directory.Exists(pf)) continue;
            var directPf = Path.Combine(pf, app.ExeName);
            if (File.Exists(directPf)) return directPf;
            foreach (var sub in subDirsToTry)
            {
                var subPf = Path.Combine(pf, sub, app.ExeName);
                if (File.Exists(subPf)) return subPf;
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // 2) Suite manifest (suite-apps.json)
        var manifestPath = SuiteManifestPath;
        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(manifestPath));
                if (manifest is not null)
                {
                    if (manifest.TryGetValue(app.ProjectName, out var exact) && File.Exists(exact))
                        return exact;
                    if (manifest.TryGetValue(app.UserFacingName, out var exactDisplay) && File.Exists(exactDisplay))
                        return exactDisplay;
                    if (manifest.TryGetValue(app.ExeName, out var exactExe) && File.Exists(exactExe))
                        return exactExe;
                }
            }
            catch { }
        }

        // 3) Active build session marker: active-build-root.txt in LocalApplicationData
        var activeMarker = Path.Combine(localAppData, "KashtrixPlayout", "active-build-root.txt");
        if (File.Exists(activeMarker))
        {
            try
            {
                var activeRoot = File.ReadAllText(activeMarker).Trim();
                if (Directory.Exists(activeRoot))
                {
                    var projDir = Path.Combine(activeRoot, "bin", app.ProjectName);
                    if (Directory.Exists(projDir))
                    {
                        var found = FindNewestExe(projDir, app.ExeName);
                        if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                    }

                    if (app.ProjectName.Equals("Kashtrix.Playout", StringComparison.OrdinalIgnoreCase))
                    {
                        var playoutDir = Path.Combine(activeRoot, "bin", "BroadcastPlayout.App");
                        if (Directory.Exists(playoutDir))
                        {
                            var found = FindNewestExe(playoutDir, "Kashtrix.Playout.exe");
                            if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                        }
                    }
                }
            }
            catch { }
        }

        // 4) Ancestor bin directory search: navigate up from AppContext.BaseDirectory to find sibling project outputs
        try
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !dir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                dir = dir.Parent;
            }
            if (dir is not null && dir.Exists)
            {
                var targetProjectDir = Path.Combine(dir.FullName, app.ProjectName);
                if (Directory.Exists(targetProjectDir))
                {
                    var found = FindNewestExe(targetProjectDir, app.ExeName);
                    if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                }

                if (app.ProjectName.Equals("Kashtrix.Playout", StringComparison.OrdinalIgnoreCase))
                {
                    var playoutDir = Path.Combine(dir.FullName, "BroadcastPlayout.App");
                    if (Directory.Exists(playoutDir))
                    {
                        var found = FindNewestExe(playoutDir, app.ExeName);
                        if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                    }
                }
            }
        }
        catch { }

        // 5) BuildSessions under LocalApplicationData\KashtrixPlayout\BuildSessions
        var sessionsRoot = Path.Combine(localAppData, "KashtrixPlayout", "BuildSessions");
        if (Directory.Exists(sessionsRoot))
        {
            try
            {
                var activeSessionBin = Path.Combine(sessionsRoot, "active", "bin", app.ProjectName);
                if (Directory.Exists(activeSessionBin))
                {
                    var found = FindNewestExe(activeSessionBin, app.ExeName);
                    if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                }

                if (app.ProjectName.Equals("Kashtrix.Playout", StringComparison.OrdinalIgnoreCase))
                {
                    var activePlayoutBin = Path.Combine(sessionsRoot, "active", "bin", "BroadcastPlayout.App");
                    if (Directory.Exists(activePlayoutBin))
                    {
                        var found = FindNewestExe(activePlayoutBin, "Kashtrix.Playout.exe");
                        if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                    }
                }

                foreach (var sessionDir in Directory.EnumerateDirectories(sessionsRoot))
                {
                    var pDir = Path.Combine(sessionDir, "bin", app.ProjectName);
                    if (Directory.Exists(pDir))
                    {
                        var found = FindNewestExe(pDir, app.ExeName);
                        if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                    }
                }
            }
            catch { }
        }

        // 6) Legacy Build\bin under LocalApplicationData\KashtrixPlayout\Build\bin
        var legacyRoot = Path.Combine(localAppData, "KashtrixPlayout", "Build", "bin", app.ProjectName);
        if (Directory.Exists(legacyRoot))
        {
            var found = FindNewestExe(legacyRoot, app.ExeName);
            if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
        }

        // 7) Source repository relative to suite-root.txt or current project working directory
        string? repoRoot = null;
        var suiteRootFile = Path.Combine(localAppData, "KashtrixPlayout", "suite-root.txt");
        if (File.Exists(suiteRootFile))
        {
            try { repoRoot = File.ReadAllText(suiteRootFile).Trim(); }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(repoRoot) || !Directory.Exists(repoRoot))
        {
            try
            {
                var cur = new DirectoryInfo(AppContext.BaseDirectory);
                while (cur is not null)
                {
                    if (File.Exists(Path.Combine(cur.FullName, "BroadcastPlayout.sln")))
                    {
                        repoRoot = cur.FullName;
                        break;
                    }
                    cur = cur.Parent;
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(repoRoot) && Directory.Exists(repoRoot))
        {
            try
            {
                var inRepo = Path.Combine(repoRoot, "src", app.ProjectName);
                if (Directory.Exists(inRepo))
                {
                    var found = FindNewestExe(inRepo, app.ExeName);
                    if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                }

                var binInRepo = Path.Combine(repoRoot, "bin", app.ProjectName);
                if (Directory.Exists(binInRepo))
                {
                    var found = FindNewestExe(binInRepo, app.ExeName);
                    if (found is not null) { UpdateManifest(app.ProjectName, found); return found; }
                }
            }
            catch { }
        }


        // 8) On-demand auto-compilation fallback in developer environment
        if (!string.IsNullOrWhiteSpace(repoRoot) && Directory.Exists(repoRoot))
        {
            try
            {
                var projPath = Path.Combine(repoRoot, "src", app.ProjectName, $"{app.ProjectName}.csproj");
                if (File.Exists(projPath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{projPath}\" -c Debug -p:Platform=x64",
                        WorkingDirectory = repoRoot,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var p = Process.Start(psi);
                    if (p is not null && p.WaitForExit(35000))
                    {
                        var activeBin = Path.Combine(sessionsRoot, "active", "bin", app.ProjectName);
                        if (Directory.Exists(activeBin))
                        {
                            var builtExe = FindNewestExe(activeBin, app.ExeName);
                            if (builtExe is not null)
                            {
                                UpdateManifest(app.ProjectName, builtExe);
                                return builtExe;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        return null;
    }

    private static string? FindNewestExe(string root, string exeName)
    {
        try
        {
            return Directory.EnumerateFiles(root, exeName, SearchOption.AllDirectories)
                .Select(p => new FileInfo(p))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Select(f => f.FullName)
                .FirstOrDefault();
        }
        catch { return null; }
    }

    private static void UpdateManifest(string projectName, string exePath)
    {
        try
        {
            var manifestDir = Path.GetDirectoryName(SuiteManifestPath);
            if (!string.IsNullOrWhiteSpace(manifestDir)) Directory.CreateDirectory(manifestDir);

            Dictionary<string, string> manifest;
            if (File.Exists(SuiteManifestPath))
            {
                try { manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SuiteManifestPath)) ?? new(); }
                catch { manifest = new(); }
            }
            else manifest = new();

            manifest[projectName] = exePath;
            File.WriteAllText(SuiteManifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static string SuiteManifestPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KashtrixPlayout", "suite-apps.json");
}
