using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BroadcastPlayout.Models;
using BroadcastPlayout.Services;

namespace Kashtrix.NRCS.Services;

public sealed class NativeBroadcastSuitePlayoutAdapter : IPlayoutProvider
{
    private readonly BroadcastDatabase _database = new();
    private readonly NrcsPlatformStore _store = new();

    public string Id => "native-kashtrix-playout";
    public string Name => "Kashtrix Native Playout Engine";
    public string Protocol => "Direct IPC / Shared DB / Control Bus";
    public ConnectorCapabilities Capabilities =>
        ConnectorCapabilities.PlayoutControl |
        ConnectorCapabilities.MediaPlayback |
        ConnectorCapabilities.RundownManagement;

    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Connected;
    public string StatusMessage { get; private set; } = "Connected to local playout channel bus";

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        Status = ConnectorStatus.Connected;
        StatusMessage = "Connected to local playout channel bus";
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        Status = ConnectorStatus.Disconnected;
        StatusMessage = "Disconnected";
        return Task.CompletedTask;
    }

    public Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        var ok = File.Exists(NrcsPlatformStore.DatabasePath);
        Status = ok ? ConnectorStatus.Healthy : ConnectorStatus.Warning;
        StatusMessage = ok ? "SQLite channel storage healthy" : "Storage path missing";
        return Task.FromResult(ok);
    }

    public Task<int> SyncRundownPlaylistAsync(string rundownId, CancellationToken ct = default)
    {
        var publishedCount = _store.PublishToPlayout(rundownId);
        return Task.FromResult(publishedCount);
    }

    public Task<bool> PlayItemAsync(string channelId, string itemId, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? "KTX-PLAYOUT-01" : channelId,
            Action = "play",
            Parameters = new Dictionary<string, string> { ["itemId"] = itemId }
        });
        return Task.FromResult(true);
    }

    public Task<bool> StopChannelAsync(string channelId, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? "KTX-PLAYOUT-01" : channelId,
            Action = "stop"
        });
        return Task.FromResult(true);
    }

    public Task<bool> NextItemAsync(string channelId, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? "KTX-PLAYOUT-01" : channelId,
            Action = "next"
        });
        return Task.FromResult(true);
    }
}

public sealed class NativeBroadcastSuiteCgAdapter : ICgGraphicsProvider
{
    private readonly BroadcastDatabase _database = new();

    public string Id => "native-kashtrix-cg";
    public string Name => "Kashtrix CG Engine & Compositor";
    public string Protocol => "Direct Engine Bus / Shared DB";
    public ConnectorCapabilities Capabilities =>
        ConnectorCapabilities.CGControl |
        ConnectorCapabilities.GraphicsTemplates |
        ConnectorCapabilities.GraphicsPreview |
        ConnectorCapabilities.GraphicsTake;

    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Connected;
    public string StatusMessage { get; private set; } = "CG engine layer bus online (Layers 0-999)";

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        Status = ConnectorStatus.Connected;
        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        Status = ConnectorStatus.Disconnected;
        return Task.CompletedTask;
    }

    public Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public IReadOnlyList<string> GetAvailableTemplates()
    {
        try
        {
            var catalog = _database.LoadState("cg-projects", new List<CgProject>());
            if (catalog.Count == 0) catalog = CgDemoFactory.CreateDefaults();
            return catalog.Select(x => x.Name).OrderBy(x => x).ToList();
        }
        catch
        {
            return CgDemoFactory.CreateDefaults().Select(x => x.Name).OrderBy(x => x).ToList();
        }
    }

    public Task<bool> PreviewGraphicAsync(string template, int layer, string dataJson, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = "KTX-PLAYOUT-01",
            Action = "cg_preview",
            Parameters = new Dictionary<string, string>
            {
                ["template"] = template,
                ["layer"] = layer.ToString(),
                ["data"] = dataJson
            }
        });
        return Task.FromResult(true);
    }

    public Task<bool> ProgramTakeGraphicAsync(string template, int layer, string dataJson, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = "KTX-PLAYOUT-01",
            Action = "cg_play",
            Parameters = new Dictionary<string, string>
            {
                ["template"] = template,
                ["layer"] = layer.ToString(),
                ["data"] = dataJson
            }
        });
        return Task.FromResult(true);
    }

    public Task<bool> ClearLayerAsync(int layer, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = "KTX-PLAYOUT-01",
            Action = "cg_stop",
            Parameters = new Dictionary<string, string>
            {
                ["layer"] = layer.ToString()
            }
        });
        return Task.FromResult(true);
    }
}

public sealed class NativeBroadcastSuitePrompterAdapter : IPrompterProvider
{
    private readonly NrcsPlatformStore _store = new();

    public string Id => "native-kashtrix-prompter";
    public string Name => "Kashtrix Prompter Sync Engine";
    public string Protocol => "SQLite WAL Live State / UDP 7788";
    public ConnectorCapabilities Capabilities =>
        ConnectorCapabilities.PrompterSync |
        ConnectorCapabilities.PrompterControl;

    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Connected;
    public string StatusMessage { get; private set; } = "Synchronized via WAL live state and run-order table";

    public Task<bool> ConnectAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task SyncLiveStoryAsync(string rundownId, string storyId, string itemId, bool onAir, CancellationToken ct = default)
    {
        _store.SetLiveStory(rundownId, storyId, itemId, onAir);
        return Task.CompletedTask;
    }

    public Task<bool> SendPrompterCommandAsync(string command, CancellationToken ct = default)
    {
        _ = PlatformControlBus.Submit(new PlatformControlCommand
        {
            ChannelId = "KTX-PLAYOUT-01",
            Action = "prompter_command",
            Parameters = new Dictionary<string, string> { ["command"] = command }
        });
        return Task.FromResult(true);
    }
}

public sealed class BuiltInAiAssistantAdapter : IAiAssistantProvider
{
    public string Id => "builtin-nrcs-ai";
    public string Name => "Kashtrix Editorial AI Assistant";
    public string Protocol => "Local Editorial Neural / NLP Engine";
    public ConnectorCapabilities Capabilities => ConnectorCapabilities.AiAssistance;

    public ConnectorStatus Status => ConnectorStatus.Healthy;
    public string StatusMessage => "Ready (Advisory only · Human approval required)";

    public Task<bool> ConnectAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<string> GenerateHeadlineAsync(string storyBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyBody)) return Task.FromResult("BREAKING: News Update");
        var lines = storyBody.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var first = lines.FirstOrDefault(x => !x.StartsWith('[') && x.Length > 10) ?? lines.FirstOrDefault() ?? "News Update";
        var headline = first.Length > 85 ? first[..82] + "..." : first;
        headline = headline.Replace(".", "").ToUpperInvariant();
        return Task.FromResult(headline);
    }

    public Task<string> GenerateSummaryAsync(string storyBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyBody)) return Task.FromResult("No story body text provided.");
        var sentences = storyBody.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                 .Where(s => !s.StartsWith('[') && s.Length > 15)
                                 .Take(3)
                                 .ToList();
        return Task.FromResult(string.Join(". ", sentences) + (sentences.Count > 0 ? "." : ""));
    }

    public Task<string> GenerateTagsAsync(string storyBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(storyBody)) return Task.FromResult("#news #broadcast");
        var words = storyBody.Split(new[] { ' ', '\r', '\n', ',', '.', ';', ':', '"', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
                             .Where(w => w.Length > 4 && !w.StartsWith('['))
                             .Select(w => w.ToLowerInvariant())
                             .GroupBy(w => w)
                             .OrderByDescending(g => g.Count())
                             .Take(5)
                             .Select(g => "#" + g.Key);
        return Task.FromResult(string.Join(" ", words));
    }

    public Task<string> SuggestKeywordsAsync(string storyBody, CancellationToken ct = default)
    {
        return GenerateTagsAsync(storyBody, ct);
    }
}

public sealed class ConnectorManager
{
    private static readonly Lazy<ConnectorManager> _instance = new(() => new ConnectorManager());
    public static ConnectorManager Instance => _instance.Value;

    private readonly List<IConnector> _connectors = new();

    public IReadOnlyList<IConnector> Connectors => _connectors;
    public IPlayoutProvider Playout { get; }
    public ICgGraphicsProvider Graphics { get; }
    public IPrompterProvider Prompter { get; }
    public IAiAssistantProvider Ai { get; }

    public ConnectorManager()
    {
        Playout = new NativeBroadcastSuitePlayoutAdapter();
        Graphics = new NativeBroadcastSuiteCgAdapter();
        Prompter = new NativeBroadcastSuitePrompterAdapter();
        Ai = new BuiltInAiAssistantAdapter();

        _connectors.Add(Playout);
        _connectors.Add(Graphics);
        _connectors.Add(Prompter);
        _connectors.Add(Ai);
    }

    public async Task<List<ConnectorStatusReport>> CheckAllHealthAsync()
    {
        var reports = new List<ConnectorStatusReport>();
        foreach (var c in _connectors)
        {
            var ok = await c.HealthCheckAsync().ConfigureAwait(false);
            reports.Add(new ConnectorStatusReport(c.Id, c.Name, c.Protocol, c.Status, c.StatusMessage));
        }
        return reports;
    }
}

public sealed record ConnectorStatusReport(string Id, string Name, string Protocol, ConnectorStatus Status, string Message);
