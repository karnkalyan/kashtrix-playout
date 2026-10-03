using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kashtrix.NRCS.Services;

[Flags]
public enum ConnectorCapabilities
{
    None = 0,
    StoryManagement = 1 << 0,
    RundownManagement = 1 << 1,
    MediaSearch = 1 << 2,
    MediaPreview = 1 << 3,
    MediaTransfer = 1 << 4,
    MediaPlayback = 1 << 5,
    GraphicsTemplates = 1 << 6,
    GraphicsEditing = 1 << 7,
    GraphicsPreview = 1 << 8,
    GraphicsTake = 1 << 9,
    CGControl = 1 << 10,
    PrompterSync = 1 << 11,
    PrompterControl = 1 << 12,
    MOSRundown = 1 << 13,
    MOSStory = 1 << 14,
    MOSObject = 1 << 15,
    Ingest = 1 << 16,
    Archive = 1 << 17,
    PlayoutControl = 1 << 18,
    Publishing = 1 << 19,
    WireFeed = 1 << 20,
    DeviceMonitoring = 1 << 21,
    AiAssistance = 1 << 22
}

public enum ConnectorStatus
{
    Healthy,
    Connecting,
    Connected,
    Warning,
    Degraded,
    Disconnected,
    Error,
    Disabled
}

public interface IConnector
{
    string Id { get; }
    string Name { get; }
    string Protocol { get; }
    ConnectorCapabilities Capabilities { get; }
    ConnectorStatus Status { get; }
    string StatusMessage { get; }
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}

public interface IPlayoutProvider : IConnector
{
    Task<int> SyncRundownPlaylistAsync(string rundownId, CancellationToken ct = default);
    Task<bool> PlayItemAsync(string channelId, string itemId, CancellationToken ct = default);
    Task<bool> StopChannelAsync(string channelId, CancellationToken ct = default);
    Task<bool> NextItemAsync(string channelId, CancellationToken ct = default);
}

public interface ICgGraphicsProvider : IConnector
{
    IReadOnlyList<string> GetAvailableTemplates();
    Task<bool> PreviewGraphicAsync(string template, int layer, string dataJson, CancellationToken ct = default);
    Task<bool> ProgramTakeGraphicAsync(string template, int layer, string dataJson, CancellationToken ct = default);
    Task<bool> ClearLayerAsync(int layer, CancellationToken ct = default);
}

public interface IPrompterProvider : IConnector
{
    Task SyncLiveStoryAsync(string rundownId, string storyId, string itemId, bool onAir, CancellationToken ct = default);
    Task<bool> SendPrompterCommandAsync(string command, CancellationToken ct = default);
}

public interface IWireProvider : IConnector
{
    Task<IReadOnlyList<WireArticle>> FetchWiresAsync(string? query = null, CancellationToken ct = default);
}

public interface IAiAssistantProvider : IConnector
{
    Task<string> GenerateHeadlineAsync(string storyBody, CancellationToken ct = default);
    Task<string> GenerateSummaryAsync(string storyBody, CancellationToken ct = default);
    Task<string> GenerateTagsAsync(string storyBody, CancellationToken ct = default);
    Task<string> SuggestKeywordsAsync(string storyBody, CancellationToken ct = default);
}

public sealed record WireArticle(
    string Id,
    string Agency,
    string Headline,
    string Body,
    DateTime PublishedUtc,
    string Urgency,
    string Category,
    string Language);
