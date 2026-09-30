using BroadcastPlayout.Models;

namespace BroadcastPlayout.Services;

public static class DemoDataFactory
{
    public static string ResolveDemoAsset(string relativeSubPath)
    {
        var normalized = relativeSubPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        
        var cand = Path.Combine(AppContext.BaseDirectory, normalized);
        if (File.Exists(cand)) return cand;

        cand = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, normalized);
        if (File.Exists(cand)) return cand;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 7 && dir != null; i++)
        {
            var p1 = Path.Combine(dir.FullName, normalized);
            if (File.Exists(p1)) return p1;

            var p2 = Path.Combine(dir.FullName, "src", "BroadcastPlayout.App", normalized);
            if (File.Exists(p2)) return p2;

            var p3 = Path.Combine(dir.FullName, "demos", normalized.StartsWith("demos" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? normalized.Substring(6) : normalized);
            if (File.Exists(p3)) return p3;

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, normalized);
    }

    public static IReadOnlyList<PlaylistItem> CreatePlaylist()
    {
        var demoVideo = ResolveDemoAsset("demos/gfx/video/kashtrix-video-layer.mp4");
        var demoThumb = ResolveDemoAsset("demos/ui/demo-city.png");
        var start = DateTime.Today.AddHours(22).AddMinutes(23).AddSeconds(45);
        var rows = new[]
        {
            ("PKG", "Graphics",   "News Intro Package",       60.0, "HD",               "/Graphics/News_Intro.mxf"),
            ("AD",  "Commercial", "Global Connect Ad",        30.0, "30s Spot",         "/Commercials/GC_30s.mov"),
            ("SEG", "News",       "Top Stories",             120.0, "Anchor: Sarah",    "/News/Top_Stories.mxf"),
            ("PKG", "News",       "Weather Forecast",         90.0, "Met Office Feed",  "/News/Weather.mxf"),
            ("BRK", "News",       "News Stinger",             20.0, "Quick CG",         "/Graphics/Stinger_01.mov"),
            ("PKG", "Feature",    "City Tonight Feature",    120.0, "VO+SOT",           "/News/City_Tonight.mxf"),
            ("AD",  "Commercial", "Fresh Mart Super Saver",   30.0, "30s Spot",         "/Commercials/FM_30s.mov"),
            ("PKG", "News",       "International Update",    180.0, "SOT Package",      "/News/Intl_Update.mxf"),
            ("LIVE","Live",       "Live Report - City Center",300.0, "Live Uplink",      "SDI 2 (RX)"),
            ("BRK", "News",       "News Stinger",             10.0, "Quick CG",         "/Graphics/Stinger_02.mov"),
            ("PKG", "News",       "Market Update",           150.0, "Finance",          "/News/Market_Update.mxf"),
            ("FILL","System",     "Program Filler",           60.0, "Filler",           "/Media/Filler_Loop.mov"),
            ("PKG", "Graphics",   "News Outro",               60.0, "HD",               "/Graphics/News_Outro.mxf")
        };

        var result = new List<PlaylistItem>();
        var seq = 1;
        var cursor = start;
        foreach (var row in rows)
        {
            var displayDuration = TimeSpan.FromSeconds(row.Item4);
            var sourceDuration = TimeSpan.FromSeconds(6);
            var live = row.Item1 == "LIVE";
            result.Add(new PlaylistItem
            {
                Sequence = seq++,
                StartTimeText = cursor.ToString("HH:mm:ss:ff"),
                EventType = row.Item1,
                Category = row.Item2,
                Title = row.Item3,
                Notes = row.Item5,
                Location = row.Item6,
                DisplayDurationText = string.Empty,
                ThumbnailPath = demoThumb,
                FilePath = demoVideo,
                SourceKind = "File",
                SourceDuration = sourceDuration,
                InPoint = TimeSpan.Zero,
                OutPoint = sourceDuration,
                SourceFrameRate = 50,
                Codec = live ? "Live simulation / Demo" : "XDCAM HD 422 / Demo",
                VideoFormat = "1920x1080i50",
                Status = "Ready"
            });
            cursor += sourceDuration;
        }

        // Demonstrate one-source/multi-part trimming without inserting the media twice.
        var feature = result.First(x => x.Title == "City Tonight Feature");
        feature.SourceDuration = TimeSpan.FromSeconds(6);
        feature.Parts = new System.Collections.ObjectModel.ObservableCollection<MediaPart>
        {
            new() { Order = 1, Name = "Opening", InPoint = TimeSpan.FromSeconds(0.2), OutPoint = TimeSpan.FromSeconds(1.4), Enabled = true },
            new() { Order = 2, Name = "Interview", InPoint = TimeSpan.FromSeconds(2.0), OutPoint = TimeSpan.FromSeconds(3.8), Enabled = true },
            new() { Order = 3, Name = "Closing", InPoint = TimeSpan.FromSeconds(4.4), OutPoint = TimeSpan.FromSeconds(5.8), Enabled = true }
        };
        return result;
    }

    public static IReadOnlyList<ScheduleEntry> CreateSchedules()
    {
        var day = DateTime.Today;
        return new[]
        {
            NewSchedule("Morning News", day.AddHours(6), "Program", "Morning Headlines", TimeSpan.FromHours(1), "Daily"),
            NewSchedule("Weather Update", day.AddHours(7).AddMinutes(15), "Media", "Weather & Traffic", TimeSpan.FromMinutes(15), "Daily"),
            NewSchedule("Business Desk", day.AddHours(9), "Program", "Business Update", TimeSpan.FromMinutes(30), "Weekdays"),
            NewSchedule("Midday Bulletin", day.AddHours(12), "Program", "National News Package", TimeSpan.FromHours(1), "Daily"),
            NewSchedule("Prime Bulletin", day.AddHours(20), "Program", "Prime Bulletin Promo", TimeSpan.FromHours(1), "Daily"),
            NewSchedule("Music Prime", day.AddHours(21).AddMinutes(30), "Program", "Music Now Playing / Coming Up Auto CG", TimeSpan.FromHours(1), "Daily"),
            NewSchedule("Overnight Filler", day.AddHours(23), "Filler", "Configured filler folder", TimeSpan.FromHours(7), "Daily")
        };
    }

    public static IReadOnlyList<ChannelDefinition> CreateChannels()
    {
        return new[]
        {
            NewChannel("KTX-CH-001", "Kashtrix News HD", "1080p50", "Main news service"),
            NewChannel("KTX-CH-002", "Kashtrix News +1", "1080p50", "Time-shift service"),
            NewChannel("KTX-CH-003", "Kashtrix Business", "1080p50", "Business service"),
            NewChannel("KTX-CH-004", "Kashtrix Sports", "1080p50", "Sports service"),
            NewChannel("KTX-CH-005", "Kashtrix Digital", "720p50", "Digital simulcast"),
            NewChannel("KTX-CH-006", "Kashtrix UHD", "2160p50", "UHD service"),
            NewChannel("KTX-MUSIC-01", "Kashtrix Music HD", "1080p50", "Music channel demo · filename metadata Auto CG")
        };
    }

    private static ScheduleEntry NewSchedule(string name, DateTime start, string type, string source, TimeSpan duration, string repeat) => new()
    {
        Name = name,
        StartAt = start,
        EventType = type,
        SourceLabel = source,
        Duration = duration,
        Repeat = repeat,
        StartMode = "Hard Start",
        Category = type,
        Enabled = true
    };

    private static ChannelDefinition NewChannel(string id, string name, string format, string notes) => new()
    {
        ChannelId = id,
        ChannelName = name,
        Format = format,
        Notes = notes,
        Enabled = true
    };
}
