using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BroadcastPlayout.Services;
using BroadcastPlayout.Models;
using BroadcastPlayout.Outputs;
using System.IO.MemoryMappedFiles;

namespace Kashtrix.ApiGateway;

public sealed class GatewaySettings
{
    public string BindHost { get; set; } = "127.0.0.1";
    public int HttpPort { get; set; } = 9080;
    public int TcpPort { get; set; } = 9090;
    public int UdpPort { get; set; } = 9091;
    public string ApiKey { get; set; } = "kashtrix-local-test";
}

public sealed class ConnectedClientInfo
{
    public string Id { get; set; } = string.Empty;
    public string ClientAddress { get; set; } = string.Empty;
    public string Protocol { get; set; } = "HTTP"; // HTTP, WS, TCP, UDP
    public string UserAgent { get; set; } = string.Empty;
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
    public int RequestCount { get; set; } = 1;
    public string LastPathOrAction { get; set; } = string.Empty;
    public int LastStatusCode { get; set; } = 200;
    public string Status { get; set; } = "Active"; // Active, Idle, Closed
    public string DisplaySummary => $"{Protocol} · {ClientAddress} · {LastPathOrAction} ({RequestCount} reqs)";
}

public sealed class GatewayHost : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = false };
    private CancellationTokenSource? _cts; private HttpListener? _http; private TcpListener? _tcp; private UdpClient? _udp;
    private readonly ConcurrentDictionary<string, ConnectedClientInfo> _clients = new();

    public GatewaySettings Settings { get; } = LoadSettings();
    public event Action<string>? Log;
    public bool IsRunning => _cts is not null;
    public static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KashtrixPlayout", "api-gateway.json");

    public IReadOnlyList<ConnectedClientInfo> GetConnectedClients()
    {
        return _clients.Values.OrderByDescending(x => x.LastActivityUtc).ToList();
    }

    private void TrackClient(string id, string address, string protocol, string? userAgent, string pathOrAction, int statusCode, string status = "Active")
    {
        _clients.AddOrUpdate(id, _ => new ConnectedClientInfo
        {
            Id = id,
            ClientAddress = address,
            Protocol = protocol,
            UserAgent = userAgent ?? string.Empty,
            ConnectedAtUtc = DateTime.UtcNow,
            LastActivityUtc = DateTime.UtcNow,
            RequestCount = 1,
            LastPathOrAction = pathOrAction,
            LastStatusCode = statusCode,
            Status = status
        }, (_, existing) =>
        {
            existing.LastActivityUtc = DateTime.UtcNow;
            existing.RequestCount++;
            existing.LastPathOrAction = pathOrAction;
            existing.LastStatusCode = statusCode;
            existing.Status = status;
            if (!string.IsNullOrWhiteSpace(userAgent)) existing.UserAgent = userAgent;
            return existing;
        });
    }

    public void Start()
    {
        if (IsRunning) return; _cts = new CancellationTokenSource();
        _http = new HttpListener(); _http.Prefixes.Add($"http://{Settings.BindHost}:{Settings.HttpPort}/"); _http.Start();
        _tcp = new TcpListener(IPAddress.Parse(Settings.BindHost), Settings.TcpPort); _tcp.Start();
        _udp = new UdpClient(new IPEndPoint(IPAddress.Parse(Settings.BindHost), Settings.UdpPort));
        _ = Task.Run(() => HttpLoop(_cts.Token)); _ = Task.Run(() => TcpLoop(_cts.Token)); _ = Task.Run(() => UdpLoop(_cts.Token));
        Write($"REST/WS http://{Settings.BindHost}:{Settings.HttpPort} · TCP {Settings.TcpPort} · UDP {Settings.UdpPort} · MCP /mcp");
    }

    public void Stop() { var c = Interlocked.Exchange(ref _cts, null); try { c?.Cancel(); } catch { } try { _http?.Stop(); } catch { } try { _tcp?.Stop(); } catch { } try { _udp?.Dispose(); } catch { } _http = null; _tcp = null; _udp = null; try { c?.Dispose(); } catch { } Write("Gateway stopped."); }

    private async Task HttpLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _http?.IsListening == true)
        {
            try { var context = await _http.GetContextAsync().WaitAsync(ct); _ = Task.Run(() => HandleHttp(context, ct), ct); } catch (OperationCanceledException) { break; } catch (HttpListenerException) { break; } catch (Exception ex) { Write("HTTP: " + ex.Message); }
        }
    }

    private async Task HandleHttp(HttpListenerContext c, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var clientIp = c.Request.RemoteEndPoint?.ToString() ?? "127.0.0.1";
        var userAgent = c.Request.Headers["User-Agent"] ?? string.Empty;
        var path = c.Request.Url?.AbsolutePath ?? "/";
        var method = c.Request.HttpMethod;
        var clientId = $"HTTP-{c.Request.RemoteEndPoint?.Address}";

        async Task RespondJson(object data, int statusCode = 200)
        {
            c.Response.StatusCode = statusCode;
            TrackClient(clientId, clientIp, "HTTP", userAgent, $"{method} {path}", statusCode);
            Write($"[HTTP {statusCode}] {method} {path} from {clientIp} ({sw.ElapsedMilliseconds}ms)");
            await WriteJson(c, data);
        }

        try
        {
            try
            {
                c.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                c.Response.AppendHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                c.Response.AppendHeader("Access-Control-Allow-Headers", "*");
                c.Response.AppendHeader("Access-Control-Allow-Private-Network", "true");
            }
            catch { }
            if (string.Equals(c.Request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                c.Response.StatusCode = 204;
                c.Response.Close();
                TrackClient(clientId, clientIp, "HTTP", userAgent, "OPTIONS preflight", 204);
                return;
            }

            if (path.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                await WriteHtml(c, DashboardHtml());
                TrackClient(clientId, clientIp, "HTTP", userAgent, "GET /", 200);
                Write($"[HTTP 200] GET / from {clientIp} ({sw.ElapsedMilliseconds}ms)");
                return;
            }
            if (path.Equals("/api/v1/health", StringComparison.OrdinalIgnoreCase))
            {
                await RespondJson(new { ok = true, service = "Kashtrix.ApiGateway", utc = DateTime.UtcNow, channels = PlatformControlBus.ReadStatuses().Count });
                return;
            }
            if (path.Equals("/api/v1/events/ws", StringComparison.OrdinalIgnoreCase) && c.Request.IsWebSocketRequest)
            {
                if (!Authorized(c.Request))
                {
                    c.Response.StatusCode = 401;
                    c.Response.Close();
                    TrackClient(clientId, clientIp, "WS", userAgent, "WS /api/v1/events/ws", 401, "Rejected");
                    Write($"[HTTP 401] WS /api/v1/events/ws from {clientIp} (Unauthorized)");
                    return;
                }
                var ws = await c.AcceptWebSocketAsync(null);
                await WebSocketSession(ws.WebSocket, clientIp, ct);
                return;
            }
            if (path.Equals("/mcp", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST")
            {
                if (!Authorized(c.Request))
                {
                    c.Response.StatusCode = 401;
                    c.Response.Close();
                    TrackClient(clientId, clientIp, "MCP", userAgent, "POST /mcp", 401, "Rejected");
                    Write($"[HTTP 401] POST /mcp from {clientIp} (Unauthorized)");
                    return;
                }
                await HandleMcp(c, ct);
                TrackClient(clientId, clientIp, "MCP", userAgent, "POST /mcp", 200);
                Write($"[HTTP 200] POST /mcp from {clientIp} ({sw.ElapsedMilliseconds}ms)");
                return;
            }
            if (!Authorized(c.Request))
            {
                await RespondJson(new { ok = false, code = "UNAUTHENTICATED" }, 401);
                return;
            }
            if (path.Equals("/api/v1/demo/load", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST")
            {
                var result = new DemoIntegrationService().SeedAndValidate();
                await RespondJson(new { ok = true, message = "Demo CG packages and broadcast rundowns loaded successfully.", data = result }, 200);
                return;
            }
            if (path.Equals("/api/v1/media/serve", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET")
            {
                await ServeMediaFileAsync(c, ct);
                return;
            }
            if (path.Equals("/api/v1/playout/channels", StringComparison.OrdinalIgnoreCase) || path.Equals("/api/v1/channels", StringComparison.OrdinalIgnoreCase))
            {
                var statuses = PlatformControlBus.ReadStatuses();
                if (statuses.Count == 0)
                {
                    GatewayPlayoutEngine.EnsureInitialized();
                    statuses = PlatformControlBus.ReadStatuses();
                    if (statuses.Count == 0)
                    {
                        statuses = new[] { GatewayPlayoutEngine.GetStatus("KTX-PLAYOUT-01") };
                    }
                }
                var list = statuses.Select(x => new
                {
                    channelId = x.ChannelId,
                    channelName = x.ChannelName,
                    engine = x.Engine,
                    currentItemId = x.CurrentItemId,
                    currentTitle = x.CurrentTitle,
                    selectedItemId = x.SelectedItemId,
                    selectedTitle = x.SelectedTitle,
                    elapsedSeconds = x.ElapsedSeconds,
                    durationSeconds = x.DurationSeconds,
                    outputArmed = x.OutputArmed,
                    haStatus = x.HaStatus,
                    capabilityStatus = x.CapabilityStatus,
                    updatedUtc = x.UpdatedUtc
                });
                await RespondJson(new { ok = true, data = list });
                return;
            }
            if (path.Equals("/api/v1/playout/playlist", StringComparison.OrdinalIgnoreCase)) { await RespondJson(FormatPlaylistForApi()); return; }
            if (path.Equals("/api/v1/playout/commands", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); var ch = doc.RootElement.TryGetProperty("channelId", out var chEl) ? chEl.GetString() ?? "KTX-PLAYOUT-01" : "KTX-PLAYOUT-01"; var resp = await DispatchJsonCommand(ch, doc, ct); await RespondJson(resp, resp.Ok ? 200 : 422); return; }
            if (path.Equals("/api/v1/playout/asrun", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET") { var channel = c.Request.QueryString["channelId"]; var limit = int.TryParse(c.Request.QueryString["limit"], out var n) ? Math.Clamp(n, 1, 10000) : 500; await RespondJson(new { ok = true, data = new AsRunLogService().List(channel, limit) }); return; }
            if (path.Equals("/api/v1/channel-controller/channels", StringComparison.OrdinalIgnoreCase)) { await RespondJson(new { ok = true, data = PlatformControlBus.ReadStatuses() }); return; }
            if (path.Equals("/api/v1/channel-controller/commands", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); await RespondJson(await DispatchMultiChannelCommand(doc, ct)); return; }
            if (path.Equals("/api/v1/cg-controller/targets", StringComparison.OrdinalIgnoreCase)) { await RespondJson(new { ok = true, data = PlatformControlBus.ReadStatuses().Select(x => new { x.ChannelId, x.ChannelName, x.Engine, x.OutputArmed, x.UpdatedUtc }) }); return; }
            if (path.Equals("/api/v1/cg-controller/onair", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET") { var chFilter = c.Request.QueryString["channelId"]; var snaps = string.IsNullOrWhiteSpace(chFilter) ? CgOnAirRegistry.ReadAll() : (CgOnAirRegistry.Read(chFilter) is { } one ? new List<CgOnAirSnapshot> { one } : new List<CgOnAirSnapshot>()); var items = snaps.SelectMany(s => s.Items).OrderBy(i => i.ChannelId).ThenBy(i => i.Layer).ToList(); await RespondJson(new { ok = true, count = items.Count, channels = snaps.Select(s => new { s.ChannelId, s.ChannelName, s.UpdatedUtc, count = s.Items.Count }), data = items }); return; }
            if ((path.Equals("/api/v1/cg-controller/preview-frame", StringComparison.OrdinalIgnoreCase) ||
                 path.Equals("/api/v1/cg-controller/program-frame", StringComparison.OrdinalIgnoreCase)) &&
                c.Request.HttpMethod == "GET")
            {
                var isProgram = path.EndsWith("program-frame", StringComparison.OrdinalIgnoreCase);
                await HandleCgControllerFrameAsync(c, isProgram, ct);
                return;
            }
            if (path.Equals("/api/v1/cg/thumbnail", StringComparison.OrdinalIgnoreCase) || path.Equals("/api/v1/cg/preview-image", StringComparison.OrdinalIgnoreCase))
            {
                var idStr = c.Request.QueryString["id"];
                var nameStr = c.Request.QueryString["name"];
                var w = int.TryParse(c.Request.QueryString["w"], out var pw) ? Math.Clamp(pw, 64, 1920) : (path.Contains("preview") ? 640 : 320);
                var h = int.TryParse(c.Request.QueryString["h"], out var ph) ? Math.Clamp(ph, 36, 1080) : (path.Contains("preview") ? 360 : 180);
                var png = await RenderCgThumbnailPngAsync(idStr, nameStr, w, h);
                if (png != null && png.Length > 0)
                {
                    c.Response.ContentType = "image/png";
                    c.Response.Headers["Cache-Control"] = "public, max-age=3600";
                    c.Response.ContentLength64 = png.Length;
                    await c.Response.OutputStream.WriteAsync(png, ct);
                    c.Response.Close();
                    return;
                }
                c.Response.StatusCode = 404;
                c.Response.Close();
                return;
            }
            if (path.Equals("/api/v1/cg/templates", StringComparison.OrdinalIgnoreCase))
            {
                var projects = LoadCgProjectsForApi(out var catalogFallback);
                await RespondJson(new
                {
                    ok = true,
                    source = catalogFallback ? "canonical-fallback" : "database",
                    count = projects.Count,
                    data = projects.Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        category = ProjectCategory(x),
                        width = x.Width,
                        height = x.Height,
                        durationSeconds = x.DurationSeconds,
                        externalLayer = x.ExternalLayer,
                        isImported = x.IsImported,
                        thumbnailUrl = $"/api/v1/cg/thumbnail?id={x.Id}&name={Uri.EscapeDataString(x.Name)}",
                        previewUrl = $"/api/v1/cg/thumbnail?id={x.Id}&name={Uri.EscapeDataString(x.Name)}&w=640&h=360",
                        layers = x.Layers.Select(l => new
                        {
                            id = l.Id,
                            name = l.Name,
                            type = l.Type,
                            text = l.Text,
                            x = l.X,
                            y = l.Y,
                            width = l.Width,
                            height = l.Height,
                            fill = l.Fill,
                            background = l.Background,
                            fontFamily = l.FontFamily,
                            fontSize = l.FontSize,
                            dataField = l.DataField,
                            visible = l.Visible,
                            animationIn = l.AnimationIn,
                            animationOut = l.AnimationOut
                        })
                    })
                });
                return;
            }
            if (path.Equals("/api/v1/cg/import", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); await RespondJson(await HandleCgImportAsync(doc.RootElement, ct)); return; }
            if (path.StartsWith("/api/v1/cg-controller/", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); var operation = path["/api/v1/cg-controller/".Length..].Trim('/'); await RespondJson(HandleCgControllerCommand(operation, doc)); return; }
            if (path.Equals("/api/v1/mam/assets", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET")
            {
                var search = c.Request.QueryString["q"];
                var limit = int.TryParse(c.Request.QueryString["limit"], out var parsedLimit) ? Math.Clamp(parsedLimit, 1, 100000) : 5000;
                await RespondJson(new { ok = true, data = new EnterpriseMamCatalog().List(limit, search) }); return;
            }
            var mamSeg = path.Trim('/').Split('/');
            if (mamSeg.Length >= 5 && mamSeg[0].Equals("api", StringComparison.OrdinalIgnoreCase) && mamSeg[1].Equals("v1", StringComparison.OrdinalIgnoreCase) && mamSeg[2].Equals("mam", StringComparison.OrdinalIgnoreCase) && mamSeg[3].Equals("assets", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(Uri.UnescapeDataString(mamSeg[4]), out var mamAssetId))
            {
                var mam = new EnterpriseMamCatalog(); var asset = mam.Get(mamAssetId);
                if (asset is null) { await RespondJson(new { ok = false, code = "NOT_FOUND", message = "MAM asset not found." }, 404); return; }
                if (mamSeg.Length == 5 && c.Request.HttpMethod == "GET") { await RespondJson(new { ok = true, data = new { asset, history = mam.History(mamAssetId, 100) } }); return; }
                if (mamSeg.Length == 6 && mamSeg[5].Equals("verify", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { await RespondJson(new { ok = true, data = await mam.VerifyIntegrityAsync(mamAssetId, ct) }); return; }
                if (mamSeg.Length == 6 && mamSeg[5].Equals("workflow", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST")
                {
                    using var doc = await ReadJson(c.Request, ct); var root = doc.RootElement;
                    if (root.TryGetProperty("approvalState", out var approval) && approval.ValueKind == JsonValueKind.String) asset.ApprovalState = approval.GetString() ?? asset.ApprovalState;
                    if (root.TryGetProperty("rightsStartUtc", out var rs)) asset.RightsStartUtc = ReadNullableUtc(rs);
                    if (root.TryGetProperty("rightsEndUtc", out var re)) asset.RightsEndUtc = ReadNullableUtc(re);
                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String) asset.Title = title.GetString() ?? asset.Title;
                    mam.UpdateWorkflow(asset, "REST API"); await RespondJson(new { ok = true, data = mam.Get(mamAssetId) }); return;
                }
            }
            if (path.Equals("/api/v1/nrcs/rundowns", StringComparison.OrdinalIgnoreCase)) { await RespondJson(new { ok = true, data = new NrcsPlatformStore().ListRundowns() }); return; }
            if (path.Equals("/api/v1/nrcs/stories", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET") { var q = c.Request.QueryString["q"]; await RespondJson(new { ok = true, data = new NrcsPlatformStore().ListStoryBank(q) }); return; }
            if (path.Equals("/api/v1/nrcs/assignments", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET") { await RespondJson(new { ok = true, data = new NrcsPlatformStore().ListAssignments() }); return; }
            if (path.Equals("/api/v1/ha/nodes", StringComparison.OrdinalIgnoreCase)) { await RespondJson(new { ok = true, data = HaControlBus.ReadSnapshot() }); return; }
            if (path.Equals("/api/v1/ha/failover", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); await RespondJson(await DispatchHaCommand(doc, true, ct)); return; }
            if (path.Equals("/api/v1/ha/release", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { using var doc = await ReadJson(c.Request, ct); await RespondJson(await DispatchHaCommand(doc, false, ct)); return; }
            var seg = path.Trim('/').Split('/');
            if (seg.Length == 6 && seg[0] == "api" && seg[1] == "v1" && seg[2] == "nrcs" && seg[3]=="rundowns" && seg[5].Equals("live", StringComparison.OrdinalIgnoreCase))
            {
                var rundownId = Uri.UnescapeDataString(seg[4]); var store = new NrcsPlatformStore();
                if (c.Request.HttpMethod == "GET") { await RespondJson(new { ok = true, data = store.GetLiveState(rundownId) }); return; }
                if (c.Request.HttpMethod == "POST")
                {
                    using var doc = await ReadJson(c.Request, ct); var root = doc.RootElement; var storyId = root.TryGetProperty("storyId", out var si) && si.ValueKind == JsonValueKind.String ? si.GetString() ?? "" : ""; var itemId = root.TryGetProperty("rundownItemId", out var ii) && ii.ValueKind == JsonValueKind.String ? ii.GetString() ?? "" : ""; var onAir = !root.TryGetProperty("onAir", out var oa) || oa.ValueKind != JsonValueKind.False; store.SetLiveStory(rundownId, storyId, itemId, onAir); await RespondJson(new { ok = true, data = store.GetLiveState(rundownId) }); return;
                }
            }
            if (seg.Length == 6 && seg[0] == "api" && seg[1] == "v1" && seg[2] == "nrcs" && seg[3] == "rundowns" && seg[5].Equals("publish", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST")
            {
                var rundownId = Uri.UnescapeDataString(seg[4]); using var doc = await ReadJson(c.Request, ct); var publishMos = doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("publishMos", out var pm) && pm.ValueKind == JsonValueKind.True;
                var store = new NrcsPlatformStore(); var count = store.PublishToPlayout(rundownId); object? mos = null; if (publishMos) { var result = await store.PublishMosAsync(rundownId, ct); mos = new { result.Path, result.Sent, result.Message }; }
                await RespondJson(new { ok = true, data = new { rundownId, playoutItems = count, mos } }); return;
            }
            if (path.StartsWith("/api/v1/prompter/", StringComparison.OrdinalIgnoreCase) && seg.Length == 4)
            {
                var rundownId = Uri.UnescapeDataString(seg[3]); var store = new NrcsPlatformStore(); var rundown = store.ListRundowns().FirstOrDefault(x => x.Id.Equals(rundownId, StringComparison.OrdinalIgnoreCase));
                if (rundown is null) { await RespondJson(new { ok = false, code = "NOT_FOUND", message = "Rundown not found." }, 404); return; }
                await RespondJson(new { ok = true, data = new { rundown, stories = store.ListStories(rundownId) } }); return;
            }
            if (seg.Length >= 6 && seg[0] == "api" && seg[1] == "v1" && seg[2] == "playout" && seg[3] == "channels")
            {
                var channel = Uri.UnescapeDataString(seg[4]); var tail = seg[5];
                if (tail.Equals("status", StringComparison.OrdinalIgnoreCase)) { var status = PlatformControlBus.ReadStatuses().FirstOrDefault(x => x.ChannelId.Equals(channel, StringComparison.OrdinalIgnoreCase)); if (status is null) { await RespondJson(new { ok = false, code = "NOT_FOUND" }, 404); } else await RespondJson(new { ok = true, data = status }); return; }
                if (tail.Equals("playlist", StringComparison.OrdinalIgnoreCase)) { await RespondJson(FormatPlaylistForApi(channel)); return; }
                if (tail.Equals("commands", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "POST") { var doc = await ReadJson(c.Request, ct); var response = await DispatchJsonCommand(channel, doc, ct); await RespondJson(response, response.Ok ? 200 : 422); return; }
            }
            // Serve media files for web preview - enables HTML5 <video> in playout-web monitors
            if (path.Equals("/api/v1/media/serve", StringComparison.OrdinalIgnoreCase) && c.Request.HttpMethod == "GET")
            {
                var filePath = c.Request.QueryString["path"];
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    await RespondJson(new { ok = false, code = "MISSING_PATH", message = "Query parameter 'path' is required." }, 400);
                    return;
                }
                filePath = Uri.UnescapeDataString(filePath);
                if (!System.IO.File.Exists(filePath))
                {
                    await RespondJson(new { ok = false, code = "FILE_NOT_FOUND", message = $"File not found: {filePath}" }, 404);
                    return;
                }
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var mime = ext switch
                {
                    ".mp4" => "video/mp4",
                    ".webm" => "video/webm",
                    ".mkv" => "video/x-matroska",
                    ".mov" => "video/quicktime",
                    ".avi" => "video/x-msvideo",
                    ".ts" => "video/mp2t",
                    ".mxf" => "application/mxf",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".bmp" => "image/bmp",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream"
                };

                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileLength = fileInfo.Length;
                    var rangeHeader = c.Request.Headers["Range"];

                    c.Response.AddHeader("Accept-Ranges", "bytes");
                    c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                    if (!string.IsNullOrWhiteSpace(rangeHeader) && rangeHeader.StartsWith("bytes="))
                    {
                        // Range request support for video seeking
                        var rangeParts = rangeHeader["bytes=".Length..].Split('-');
                        var rangeStart = long.TryParse(rangeParts[0], out var rs) ? rs : 0;
                        var rangeEnd = rangeParts.Length > 1 && long.TryParse(rangeParts[1], out var re) ? re : fileLength - 1;
                        if (rangeEnd >= fileLength) rangeEnd = fileLength - 1;
                        var contentLength = rangeEnd - rangeStart + 1;

                        c.Response.StatusCode = 206;
                        c.Response.ContentType = mime;
                        c.Response.ContentLength64 = contentLength;
                        c.Response.AddHeader("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{fileLength}");

                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.Seek(rangeStart, SeekOrigin.Begin);
                        var buffer = new byte[65536];
                        var remaining = contentLength;
                        while (remaining > 0)
                        {
                            var toRead = (int)Math.Min(buffer.Length, remaining);
                            var read = await fs.ReadAsync(buffer.AsMemory(0, toRead), ct);
                            if (read == 0) break;
                            await c.Response.OutputStream.WriteAsync(buffer.AsMemory(0, read), ct);
                            remaining -= read;
                        }
                    }
                    else
                    {
                        c.Response.StatusCode = 200;
                        c.Response.ContentType = mime;
                        c.Response.ContentLength64 = fileLength;

                        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        await fs.CopyToAsync(c.Response.OutputStream, ct);
                    }
                    c.Response.Close();
                    return;
                }
                catch (Exception ex)
                {
                    try { await RespondJson(new { ok = false, code = "MEDIA_ERROR", message = ex.Message }, 500); } catch { }
                    return;
                }
            }
            await RespondJson(new { ok = false, code = "NOT_FOUND", path }, 404);
        }
        catch (Exception ex)
        {
            try { await RespondJson(new { ok = false, code = "INTERNAL_ERROR", message = ex.Message }, 500); } catch { }
            Write("HTTP handler exception: " + ex.Message);
        }
    }

    private bool Authorized(HttpListenerRequest r)
    {
        var key = r.Headers["X-Kashtrix-Api-Key"];
        if (string.IsNullOrWhiteSpace(key)) key = r.QueryString["apiKey"];
        if (string.IsNullOrWhiteSpace(key)) key = r.QueryString["api_key"];
        var auth = r.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(key) && auth is not null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) key = auth[7..].Trim();
        if (string.Equals(key, Settings.ApiKey, StringComparison.Ordinal) || string.Equals(key, "kashtrix-local-test", StringComparison.Ordinal)) return true; // legacy local admin key / default
        if (r.IsLocal && string.Equals(r.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) return true; // Allow local dashboard reads
        if (string.IsNullOrWhiteSpace(key)) return false;
        try
        {
            var security = new SecurityStore();
            if (!security.VerifyApiToken(key, out var principal) || principal is null) return false;
            return SecurityStore.HasScope(principal, RequiredScope(r.Url?.AbsolutePath ?? "/", r.HttpMethod));
        }
        catch { return false; }
    }

    private static string RequiredScope(string path, string method)
    {
        var write = !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
        if (path.Equals("/mcp", StringComparison.OrdinalIgnoreCase)) return "mcp:access";
        if (path.Contains("/cg", StringComparison.OrdinalIgnoreCase)) return write ? "cg:write" : "cg:read";
        if (path.Contains("/mam", StringComparison.OrdinalIgnoreCase)) return write ? "mam:write" : "mam:read";
        if (path.Contains("/nrcs", StringComparison.OrdinalIgnoreCase)) return write ? "nrcs:write" : "nrcs:read";
        if (path.Contains("/ha/", StringComparison.OrdinalIgnoreCase)) return write ? "ha:write" : "ha:read";
        if (path.Contains("prompter", StringComparison.OrdinalIgnoreCase)) return write ? "prompter:write" : "prompter:read";
        return write ? "playout:write" : "playout:read";
    }

    private bool AuthorizedToken(string? key, string requiredScope)
    {
        if (string.Equals(key, Settings.ApiKey, StringComparison.Ordinal) || string.Equals(key, "kashtrix-local-test", StringComparison.Ordinal)) return true;
        if (string.IsNullOrWhiteSpace(key)) return false;
        try { var security = new SecurityStore(); return security.VerifyApiToken(key, out var p) && p is not null && SecurityStore.HasScope(p, requiredScope); } catch { return false; }
    }

    private async Task WebSocketSession(WebSocket ws, string clientIp, CancellationToken serverCt)
    {
        var wsId = $"WS-{clientIp}";
        TrackClient(wsId, clientIp, "WS", "WebSocket Bus Client", "CONNECTED", 101, "Active");
        Write($"[WS CONNECT] WebSocket client connected: {clientIp}");

        using (ws)
        {
            var receive = new byte[65536]; using var linked = CancellationTokenSource.CreateLinkedTokenSource(serverCt); var sendTask = Task.Run(async () => { while (!linked.IsCancellationRequested && ws.State == WebSocketState.Open) { var payload = JsonSerializer.SerializeToUtf8Bytes(new { type = "event", name = "platform.status", ts = DateTime.UtcNow, data = PlatformControlBus.ReadStatuses() }, Json); await ws.SendAsync(payload, WebSocketMessageType.Text, true, linked.Token); await Task.Delay(1000, linked.Token); } }, linked.Token);
            try
            {
                while (ws.State == WebSocketState.Open && !linked.IsCancellationRequested)
                {
                    var r = await ws.ReceiveAsync(receive, linked.Token);
                    if (r.MessageType == WebSocketMessageType.Close) break;
                    var text = Encoding.UTF8.GetString(receive, 0, r.Count);
                    using var d = JsonDocument.Parse(text);
                    if (d.RootElement.TryGetProperty("type", out var t) && t.GetString() == "command")
                    {
                        var channel = d.RootElement.TryGetProperty("channelId", out var ch) ? ch.GetString() ?? "" : "";
                        var resp = await DispatchJsonCommand(channel, d, linked.Token);
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(resp, Json);
                        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, linked.Token);
                        TrackClient(wsId, clientIp, "WS", "WebSocket Bus Client", $"CMD {channel}", resp.Ok ? 200 : 422, "Active");
                        Write($"[WS CMD] {channel} from {clientIp} -> {(resp.Ok ? "OK" : "FAILED")}");
                    }
                }
            }
            catch { }
            finally
            {
                linked.Cancel();
                TrackClient(wsId, clientIp, "WS", "WebSocket Bus Client", "DISCONNECTED", 1000, "Closed");
                Write($"[WS DISCONNECT] WebSocket client disconnected: {clientIp}");
                try { await sendTask; } catch { }
                try { if (ws.State == WebSocketState.Open) await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { }
            }
        }
    }

    private async Task TcpLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { var client = await _tcp!.AcceptTcpClientAsync(ct); _ = Task.Run(() => TcpClientSession(client, ct), ct); } catch (OperationCanceledException) { break; } catch (ObjectDisposedException) { break; } catch (Exception ex) { Write("TCP: " + ex.Message); }
        }
    }
    private async Task TcpClientSession(TcpClient client, CancellationToken serverCt)
    {
        var tcpEp = client.Client.RemoteEndPoint?.ToString() ?? "TCP-Client";
        var tcpId = $"TCP-{tcpEp}";
        TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", "CONNECTED", 0, "Connected");
        Write($"[TCP CONNECT] Client connected: {tcpEp}");

        using var clientLifetime = client;
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 8192, true);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 8192, true) { AutoFlush = true };
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(serverCt);
        using var writerGate = new SemaphoreSlim(1, 1);
        var authed = false;
        Task? publisher = null;

        async Task SendLine(object value)
        {
            await writerGate.WaitAsync(linked.Token);
            try { await writer.WriteLineAsync(JsonSerializer.Serialize(value, Json)); }
            finally { writerGate.Release(); }
        }

        try
        {
            while (!linked.IsCancellationRequested && client.Connected)
            {
                string? line;
                try { line = await reader.ReadLineAsync(linked.Token); } catch { break; }
                if (line is null) break;
                try
                {
                    using var d = JsonDocument.Parse(line); var root = d.RootElement;
                    if (!authed)
                    {
                        authed = root.TryGetProperty("type", out var ty) && ty.GetString() == "auth" && root.TryGetProperty("apiKey", out var k) && AuthorizedToken(k.GetString(), "playout:write");
                        await SendLine(new { ok = authed, code = authed ? "OK" : "UNAUTHENTICATED" });
                        TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", authed ? "AUTH OK" : "AUTH FAIL", authed ? 200 : 401, authed ? "Active" : "Rejected");
                        Write($"[TCP AUTH] {tcpEp} -> {(authed ? "AUTHORIZED" : "REJECTED")}");
                        if (!authed) break;
                        continue;
                    }
                    if (root.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "subscribe")
                    {
                        await SendLine(new { ok = true, code = "SUBSCRIBED", topics = root.TryGetProperty("topics", out var topics) ? topics.Clone() : default });
                        TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", "SUBSCRIBE", 200, "Active");
                        if (publisher is null)
                        {
                            publisher = Task.Run(async () =>
                            {
                                while (!linked.IsCancellationRequested && client.Connected)
                                {
                                    await SendLine(new { type = "event", name = "platform.status", ts = DateTime.UtcNow, data = PlatformControlBus.ReadStatuses() });
                                    await Task.Delay(1000, linked.Token);
                                }
                            }, linked.Token);
                        }
                        continue;
                    }
                    if (root.TryGetProperty("type", out typeElement) && typeElement.GetString() == "ping")
                    {
                        await SendLine(new { type = "pong", ts = DateTime.UtcNow });
                        TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", "PING/PONG", 200, "Active");
                        continue;
                    }
                    var channel = root.TryGetProperty("channelId", out var ch) ? ch.GetString() ?? "" : "";
                    var resp = await DispatchJsonCommand(channel, d, linked.Token);
                    await SendLine(resp);
                    TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", $"CMD {channel}", resp.Ok ? 200 : 422, "Active");
                    Write($"[TCP CMD] {channel} from {tcpEp} -> {(resp.Ok ? "OK" : "FAILED")}");
                }
                catch (Exception ex) { await SendLine(new { ok = false, code = "INVALID_REQUEST", message = ex.Message }); }
            }
        }
        finally
        {
            linked.Cancel();
            TrackClient(tcpId, tcpEp, "TCP", "TCP Line Bus", "DISCONNECTED", 0, "Closed");
            Write($"[TCP DISCONNECT] Client disconnected: {tcpEp}");
            if (publisher is not null) try { await publisher; } catch { }
        }
    }

    private async Task UdpLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var r = await _udp!.ReceiveAsync(ct);
                using var d = JsonDocument.Parse(r.Buffer);
                var root = d.RootElement;
                var udpEp = r.RemoteEndPoint.ToString();
                var udpId = $"UDP-{udpEp}";
                if (!root.TryGetProperty("apiKey", out var key) || !AuthorizedToken(key.GetString(), "playout:write"))
                {
                    TrackClient(udpId, udpEp, "UDP", "UDP Datagram", "AUTH FAIL", 401, "Rejected");
                    Write($"[UDP 401] Unauthorized packet from {udpEp}");
                    continue;
                }
                var channel = root.TryGetProperty("channelId", out var ch) ? ch.GetString() ?? "" : "";
                var action = root.TryGetProperty("action", out var a) ? a.GetString() ?? "" : "";
                if (action is "take" or "output_arm") continue;
                var resp = await DispatchJsonCommand(channel, d, ct);
                TrackClient(udpId, udpEp, "UDP", "UDP Datagram", $"{action} on {channel}", resp.Ok ? 200 : 422, "Active");
                Write($"[UDP] Action '{action}' on '{channel}' from {udpEp} -> {(resp.Ok ? "OK" : "FAILED")}");
                if (root.TryGetProperty("ackRequested", out var ack) && ack.ValueKind == JsonValueKind.True)
                {
                    var b = JsonSerializer.SerializeToUtf8Bytes(resp, Json);
                    await _udp.SendAsync(b, r.RemoteEndPoint, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex) { Write("UDP: " + ex.Message); }
        }
    }

    private async Task<PlatformControlResponse> DispatchJsonCommand(string channel, JsonDocument d, CancellationToken ct)
    {
        var root = d.RootElement; var action = root.TryGetProperty("action", out var a) ? a.GetString() ?? "" : root.TryGetProperty("params", out var p) && p.TryGetProperty("action", out var pa) ? pa.GetString() ?? "" : "";
        var id = root.TryGetProperty("id", out var i) ? i.GetString() ?? Guid.NewGuid().ToString("N") : root.TryGetProperty("requestId", out var ri) ? ri.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
        var pars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("params", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Object) foreach (var prop in paramsEl.EnumerateObject()) pars[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? "" : prop.Value.ToString();
        foreach (var name in new[] { "itemId", "offsetSeconds", "armed" }) if (root.TryGetProperty(name, out var el)) pars[name] = el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
        if (string.IsNullOrWhiteSpace(channel)) return new PlatformControlResponse { Id = id, Ok = false, Code = "INVALID_REQUEST", Message = "channelId is required." };
        try
        {
            var res = await PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { Id = id, ChannelId = channel, Action = action, Parameters = pars }, TimeSpan.FromMilliseconds(400), ct);
            if (res != null && res.Ok) return res;
        }
        catch { }
        return GatewayPlayoutEngine.Execute(channel, action, pars, id);
    }

    private async Task<object> DispatchMultiChannelCommand(JsonDocument document, CancellationToken ct)
    {
        var root = document.RootElement;
        if (!root.TryGetProperty("targets", out var targetsElement) || targetsElement.ValueKind != JsonValueKind.Array)
            return new { ok = false, code = "INVALID_REQUEST", message = "targets array is required." };
        var action = root.TryGetProperty("action", out var actionElement) ? actionElement.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(action)) return new { ok = false, code = "INVALID_REQUEST", message = "action is required." };
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("params", out var paramsElement) && paramsElement.ValueKind == JsonValueKind.Object)
            foreach (var property in paramsElement.EnumerateObject()) parameters[property.Name] = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString();
        var requestId = root.TryGetProperty("requestId", out var req) ? req.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
        var channels = targetsElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var tasks = channels.Select(channel => PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { Id = requestId + "-" + channel, ChannelId = channel, Action = action, Parameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase) }, TimeSpan.FromSeconds(3), ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        return new { ok = results.All(x => x.Ok), accepted = results.Count(x => x.Ok), failed = results.Count(x => !x.Ok), results };
    }

    private async Task<object> DispatchHaCommand(JsonDocument document, bool forceActive, CancellationToken ct)
    {
        var root = document.RootElement;
        var channelId = root.TryGetProperty("channelId", out var ch) ? ch.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrWhiteSpace(channelId)) return new { ok = false, code = "INVALID_REQUEST", message = "channelId is required." };
        var nodeId = root.TryGetProperty("targetNode", out var tn) ? tn.GetString() ?? string.Empty : root.TryGetProperty("nodeId", out var ni) ? ni.GetString() ?? string.Empty : string.Empty;
        if (forceActive && string.IsNullOrWhiteSpace(nodeId)) return new { ok = false, code = "INVALID_REQUEST", message = "targetNode/nodeId is required." };
        var duration = root.TryGetProperty("durationSeconds", out var du) && du.TryGetInt32(out var sec) ? Math.Clamp(sec, 5, 86400) : 600;
        var reason = root.TryGetProperty("reason", out var re) ? re.GetString() ?? string.Empty : string.Empty;
        var response = await HaControlBus.SubmitAndWaitAsync(new HaControlCommand { Action = forceActive ? "force_active" : "release", ChannelId = channelId, NodeId = nodeId, DurationSeconds = duration, Reason = reason }, TimeSpan.FromSeconds(3), ct);
        string? targetNode = forceActive ? nodeId : null;
        var data = new { channelId, targetNode, durationSeconds = forceActive ? duration : 0 };
        return new { ok = response.Ok, code = response.Code, message = response.Message, data };
    }

    private object HandleCgControllerCommand(string operation, JsonDocument document)
    {
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return new { ok = false, code = "INVALID_REQUEST", message = "CG Controller request must be a JSON object." };

        var targets = ReadCgTargets(root);
        if (targets.Count == 0) return new { ok = false, code = "INVALID_REQUEST", message = "At least one CG target is required." };
        var bus = ReadJsonString(root, "bus", "PROGRAM").Trim().ToUpperInvariant();
        if (bus is not "PROGRAM" and not "PREVIEW")
            return new { ok = false, code = "INVALID_REQUEST", message = "bus must be PROGRAM or PREVIEW." };
        var layer = root.TryGetProperty("layer", out var layerEl) && layerEl.TryGetInt32(out var l) ? Math.Clamp(l, 0, 999) : -1;
        var action = operation.ToLowerInvariant() switch { "play" => "PLAY", "update" => "UPDATE", "take" => "TAKE", "stop" => "STOP", "clear" => "CLEAR", _ => string.Empty };
        if (string.IsNullOrWhiteSpace(action)) return new { ok = false, code = "NOT_FOUND", message = "Unknown CG Controller operation." };

        var failures = new List<string>();
        if (action is "STOP" or "CLEAR" or "TAKE")
        {
            var takeTemplate = ReadJsonString(root, "template", string.Empty).Trim();
            var delivered = 0;
            foreach (var target in targets)
            {
                try
                {
                    var projTarget = action is "STOP" or "CLEAR" ? BuildStopTarget(root) : null;
                    if (action is "TAKE" && !string.IsNullOrWhiteSpace(takeTemplate))
                    {
                        var all = LoadCgProjectsForApi(out _);
                        projTarget = all.FirstOrDefault(x => x.Name.Equals(takeTemplate, StringComparison.OrdinalIgnoreCase) || x.Id.ToString().Equals(takeTemplate, StringComparison.OrdinalIgnoreCase));
                    }
                    var count = LocalCgCommandBus.Publish(new[] { target.ChannelId }, new CgRemoteCommand { CommandId = Guid.NewGuid().ToString("N"), Action = action, Bus = bus, Layer = layer, Project = projTarget });
                    delivered += count;
                    if (count != 1) failures.Add(target.ChannelId + ": command was not queued");

                    if (bus == "PROGRAM")
                    {
                        var snap = CgOnAirRegistry.Read(target.ChannelId) ?? new CgOnAirSnapshot { ChannelId = target.ChannelId, ChannelName = target.ChannelId };
                        if (action is "CLEAR" or "STOP")
                        {
                            if (layer == -1) snap.Items.Clear();
                            else snap.Items.RemoveAll(x => x.Layer == layer);
                            CgOnAirRegistry.Publish(snap);
                        }
                        else if (action is "TAKE" && projTarget != null)
                        {
                            var item = CgOnAirRegistry.FromProject(target.ChannelId, projTarget, "CONTROLLER");
                            item.Layer = layer > 0 ? layer : 1;
                            snap.Items.RemoveAll(x => x.Layer == item.Layer);
                            snap.Items.Add(item);
                            CgOnAirRegistry.Publish(snap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(target.ChannelId + ": " + ex.GetBaseException().Message);
                    Write("CG " + action + " delivery failed for " + target.ChannelId + ": " + ex.GetBaseException().Message);
                }
            }
            return new { ok = delivered == targets.Count, delivered, failed = targets.Count - delivered, action, bus, layer, failures };
        }

        var template = ReadJsonString(root, "template", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(template)) return new { ok = false, code = "INVALID_REQUEST", message = "template is required." };
        var projects = LoadCgProjectsForApi(out var catalogFallback);
        var source = projects.FirstOrDefault(x =>
            x.Name.Equals(template, StringComparison.OrdinalIgnoreCase) ||
            x.Id.ToString().Equals(template, StringComparison.OrdinalIgnoreCase));
        if (source is null) return new { ok = false, code = "NOT_FOUND", message = "CG template/project not found: " + template };

        var deliveredCount = 0;
        foreach (var target in targets)
        {
            try
            {
                var project = CloneCgProject(source);
                project.ExternalLayer = layer;
                ApplyCgData(project, target.DataJson ?? ReadRootDataJson(root));
                var count = LocalCgCommandBus.Publish(new[] { target.ChannelId }, new CgRemoteCommand { CommandId = Guid.NewGuid().ToString("N"), Action = action, Bus = bus, Layer = layer, Project = project });
                deliveredCount += count;
                if (count != 1) failures.Add(target.ChannelId + ": command was not queued");

                if (bus == "PROGRAM" && action == "PLAY")
                {
                    var snap = CgOnAirRegistry.Read(target.ChannelId) ?? new CgOnAirSnapshot { ChannelId = target.ChannelId, ChannelName = target.ChannelId };
                    var item = CgOnAirRegistry.FromProject(target.ChannelId, project, "CONTROLLER");
                    snap.Items.RemoveAll(x => x.Layer == project.ExternalLayer);
                    snap.Items.Add(item);
                    CgOnAirRegistry.Publish(snap);
                }
            }
            catch (Exception ex)
            {
                failures.Add(target.ChannelId + ": " + ex.GetBaseException().Message);
                Write("CG " + action + " delivery failed for " + target.ChannelId + ": " + ex.GetBaseException().Message);
            }
        }
        return new { ok = deliveredCount == targets.Count, delivered = deliveredCount, failed = targets.Count - deliveredCount, action, bus, layer, template, source = catalogFallback ? "canonical-fallback" : "database", failures };
    }

    private static string ProjectCategory(CgProject project)
    {
        if (project.IsImported || string.Equals(project.Category, "IMPORTED", StringComparison.OrdinalIgnoreCase))
            return "IMPORTED";

        var cat = (project.Category ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(cat) && !cat.Equals("CUSTOM", StringComparison.OrdinalIgnoreCase) && !cat.Equals("GENERAL", StringComparison.OrdinalIgnoreCase))
            return cat.ToUpperInvariant();

        return !string.IsNullOrWhiteSpace(project.Category) ? project.Category.Trim().ToUpperInvariant() : "GENERAL";
    }

    private static object FormatPlaylistForApi(string? channelId = null)
    {
        var db = new BroadcastDatabase();
        var list = db.LoadPlaylist();
        var statuses = PlatformControlBus.ReadStatuses();
        var status = string.IsNullOrWhiteSpace(channelId)
            ? statuses.FirstOrDefault()
            : statuses.FirstOrDefault(x => x.ChannelId.Equals(channelId, StringComparison.OrdinalIgnoreCase));

        var currentId = status?.CurrentItemId ?? string.Empty;
        var currentTitle = status?.CurrentTitle ?? string.Empty;
        var selectedId = status?.SelectedItemId ?? string.Empty;
        var selectedTitle = status?.SelectedTitle ?? string.Empty;

        var mapped = list.Select((item, idx) =>
        {
            var idStr = item.Id.ToString("D");
            var isCurrent = (!string.IsNullOrWhiteSpace(currentId) && string.Equals(idStr, currentId, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrWhiteSpace(currentTitle) && string.Equals(item.Title, currentTitle, StringComparison.OrdinalIgnoreCase));
            var isSelected = !isCurrent && ((!string.IsNullOrWhiteSpace(selectedId) && string.Equals(idStr, selectedId, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrWhiteSpace(selectedTitle) && string.Equals(item.Title, selectedTitle, StringComparison.OrdinalIgnoreCase)));

            var itemStatus = isCurrent ? "On Air" : isSelected ? "Cued" : (!string.IsNullOrWhiteSpace(item.Status) ? item.Status : "Ready");

            var durSec = item.Duration.TotalSeconds > 0 ? item.Duration.TotalSeconds : (item.SourceDuration.TotalSeconds > 0 ? item.SourceDuration.TotalSeconds : 30.0);

            return new
            {
                id = idStr,
                sequence = item.Sequence > 0 ? item.Sequence : (idx + 1),
                title = !string.IsNullOrWhiteSpace(item.Title) ? item.Title : (!string.IsNullOrWhiteSpace(item.FilePath) ? Path.GetFileName(item.FilePath) : $"Item #{idx + 1}"),
                filePath = item.FilePath ?? string.Empty,
                slug = !string.IsNullOrWhiteSpace(item.FilePath) ? Path.GetFileName(item.FilePath) : (item.Title ?? $"Item #{idx + 1}"),
                status = itemStatus,
                inPoint = item.InPoint.ToString(@"hh\:mm\:ss"),
                outPoint = item.OutPoint.ToString(@"hh\:mm\:ss"),
                durationSeconds = Math.Round(durSec, 3),
                duration = item.Duration.ToString(@"hh\:mm\:ss"),
                eventType = item.EventType ?? "Video",
                isLiveSource = item.IsLiveSource,
                codec = item.Codec ?? string.Empty,
                videoFormat = item.VideoFormat ?? string.Empty,
                category = item.Category ?? "General"
            };
        }).ToList();

        return new { ok = true, channel = channelId ?? status?.ChannelId ?? "KTX-PLAYOUT-01", count = mapped.Count, data = mapped };
    }

    private async Task<object> HandleCgImportAsync(JsonElement root, CancellationToken ct)
    {
        try
        {
            var name = ReadJsonString(root, "name", "Imported CG");
            var base64Data = ReadJsonString(root, "base64", "");
            var jsonContent = ReadJsonString(root, "json", "");
            CgProject? imported = null;

            if (!string.IsNullOrWhiteSpace(base64Data))
            {
                var bytes = Convert.FromBase64String(base64Data);
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.kcg");
                await File.WriteAllBytesAsync(tempFile, bytes, ct);
                imported = KashtrixCgFileService.LoadComposition(tempFile);
                try { File.Delete(tempFile); } catch { }
            }
            else if (!string.IsNullOrWhiteSpace(jsonContent))
            {
                imported = JsonSerializer.Deserialize<CgProject>(jsonContent, Json);
            }
            else if (root.TryGetProperty("project", out var projEl))
            {
                imported = JsonSerializer.Deserialize<CgProject>(projEl.GetRawText(), Json);
            }

            if (imported is null)
            {
                return new { ok = false, code = "INVALID_PAYLOAD", message = "Could not parse CG composition." };
            }

            if (string.IsNullOrWhiteSpace(imported.Name)) imported.Name = name;
            imported.IsImported = true;
            imported.Category = "IMPORTED";
            imported.Id = Guid.NewGuid();

            // Persist to database state
            var db = new BroadcastDatabase();
            var existingList = db.LoadState("cg-projects", new List<CgProject>());
            existingList.RemoveAll(x => x.Name.Equals(imported.Name, StringComparison.OrdinalIgnoreCase));
            existingList.Add(imported);
            db.SaveState("cg-projects", existingList);

            // Save .kcg to demo directory
            try
            {
                var demoDir = CgDemoFactory.GetCgDemoDirectory();
                Directory.CreateDirectory(demoDir);
                var safe = string.Concat(imported.Name.Split(Path.GetInvalidFileNameChars())).Trim();
                var targetFile = Path.Combine(demoDir, safe + ".kcg");
                KashtrixCgFileService.SaveComposition(targetFile, imported);
            }
            catch { }

            Write($"[CG IMPORT] Imported template '{imported.Name}' ({imported.Layers.Count} layers)");
            return new { ok = true, message = $"CG template '{imported.Name}' successfully imported.", data = new { id = imported.Id, name = imported.Name, category = "IMPORTED" } };
        }
        catch (Exception ex)
        {
            return new { ok = false, code = "IMPORT_FAILED", message = ex.Message };
        }
    }

    private List<CgProject> LoadCgProjectsForApi(out bool canonicalFallback)
    {
        canonicalFallback = false;
        var result = new List<CgProject>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Strictly scan physical .kcg and .kashtrixcg composition files from cg-demo (and demos)
        try
        {
            var candidateDirs = new List<string>();
            var rootCgDemo = @"c:\Users\karnk\Downloads\Kashtrix-COMPLETE-PROJECT-FIX49H-FULL-BUILD-DEBUG-RECOVERY-FINAL\Kashtrix-FIX49H\cg-demo";
            if (Directory.Exists(rootCgDemo)) candidateDirs.Add(rootCgDemo);
            var d1 = CgDemoFactory.GetCgDemoDirectory();
            if (!string.IsNullOrWhiteSpace(d1) && Directory.Exists(d1) && !candidateDirs.Contains(d1, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(d1);
            var d2 = Path.Combine(Directory.GetCurrentDirectory(), "cg-demo");
            if (Directory.Exists(d2) && !candidateDirs.Contains(d2, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(d2);
            var d3 = Path.Combine(Directory.GetCurrentDirectory(), "demos");
            if (Directory.Exists(d3) && !candidateDirs.Contains(d3, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(d3);
            var d4 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KashtrixPlayout", "CgProjects");
            if (Directory.Exists(d4) && !candidateDirs.Contains(d4, StringComparer.OrdinalIgnoreCase)) candidateDirs.Add(d4);

            foreach (var dir in candidateDirs)
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
                {
                    var ext = Path.GetExtension(file);
                    if (!ext.Equals(".kcg", StringComparison.OrdinalIgnoreCase) && !ext.Equals(".kashtrixcg", StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        var p = KashtrixCgFileService.LoadComposition(file);
                        if (p != null && !string.IsNullOrWhiteSpace(p.Name) && seenNames.Add(p.Name))
                        {
                            result.Add(p);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        // 2. Custom/imported operator projects from database (excluding legacy synthetic demos)
        try
        {
            var dbProjects = new BroadcastDatabase().LoadState("cg-projects", new List<CgProject>());
            foreach (var p in dbProjects)
            {
                if (p != null && !string.IsNullOrWhiteSpace(p.Name) && !p.Name.StartsWith("Demo ", StringComparison.OrdinalIgnoreCase) && seenNames.Add(p.Name))
                {
                    result.Add(p);
                }
            }
        }
        catch (Exception ex)
        {
            Write("CG catalog database read warning: " + ex.GetBaseException().Message);
        }

        // 3. Merged canonical defaults from cg-demo if empty
        if (result.Count == 0)
        {
            canonicalFallback = true;
            return CgDemoFactory.CreateDefaults();
        }

        return result;
    }

    private static readonly ConcurrentDictionary<string, byte[]> _cgThumbnailCache = new(StringComparer.OrdinalIgnoreCase);

    private async Task<byte[]?> RenderCgThumbnailPngAsync(string? idStr, string? nameStr, int width, int height)
    {
        var cacheKey = $"{idStr}_{nameStr}_{width}x{height}";
        if (_cgThumbnailCache.TryGetValue(cacheKey, out var cached)) return cached;

        var projects = LoadCgProjectsForApi(out _);
        CgProject? project = null;
        if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var gid))
        {
            project = projects.FirstOrDefault(x => x.Id == gid);
        }
        if (project == null && !string.IsNullOrWhiteSpace(nameStr))
        {
            project = projects.FirstOrDefault(x => string.Equals(x.Name, nameStr, StringComparison.OrdinalIgnoreCase))
                ?? projects.FirstOrDefault(x => x.Name.Contains(nameStr, StringComparison.OrdinalIgnoreCase));
        }
        if (project == null) return null;

        try
        {
            var png = await Task.Run(() => RenderProjectToPngBytes(project, width, height));
            if (png != null && png.Length > 0)
            {
                _cgThumbnailCache[cacheKey] = png;
                return png;
            }
        }
        catch (Exception ex)
        {
            Write($"Thumbnail render error for '{project.Name}': {ex.Message}");
        }
        return null;
    }

    private static readonly ConcurrentDictionary<string, (long Sequence, byte[] Jpeg, DateTime Timestamp)> _cgFrameCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, byte[]> _cgPlaceholderCache = new(StringComparer.OrdinalIgnoreCase);

    private async Task HandleCgControllerFrameAsync(HttpListenerContext c, bool isProgram, CancellationToken ct)
    {
        var mapName = isProgram ? VirtualOutputBridge.CgProgramVideoMapName : VirtualOutputBridge.CustomPreviewVideoMapName;
        var label = isProgram ? "PROGRAM" : "PREVIEW";
        var jpeg = GetOrReadCgFrameJpeg(mapName, label);

        // Fallback for PROGRAM: if dedicated CG program bridge has no active frames, try main playout program bridge
        if (isProgram && _cgFrameCache.TryGetValue(mapName, out var entry) && entry.Jpeg == null)
        {
            var fallbackJpeg = GetOrReadCgFrameJpeg(VirtualOutputBridge.VideoMapName, "PROGRAM");
            if (fallbackJpeg != null)
            {
                jpeg = fallbackJpeg;
                mapName = VirtualOutputBridge.VideoMapName;
            }
        }

        var seq = _cgFrameCache.TryGetValue(mapName, out var cached) ? cached.Sequence : 0;
        var etag = $"\"{seq}\"";
        if (seq > 0 && string.Equals(c.Request.Headers["If-None-Match"], etag, StringComparison.Ordinal))
        {
            c.Response.StatusCode = 304;
            c.Response.Close();
            return;
        }

        c.Response.StatusCode = 200;
        c.Response.ContentType = "image/jpeg";
        if (seq > 0) c.Response.Headers["ETag"] = etag;
        c.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        c.Response.Headers["Pragma"] = "no-cache";
        c.Response.Headers["Expires"] = "0";
        c.Response.ContentLength64 = jpeg.Length;
        await c.Response.OutputStream.WriteAsync(jpeg, ct);
        c.Response.Close();
    }

    private static byte[] GetOrReadCgFrameJpeg(string mapName, string label)
    {
        try
        {
            using var map = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            if (view.ReadInt32(0) == 0x4B545856) // KTXV
            {
                var width = view.ReadInt32(8);
                var height = view.ReadInt32(12);
                var stride = view.ReadInt32(16);
                var sequence = view.ReadInt64(28);
                var length = view.ReadInt32(44);

                if (width > 0 && height > 0 && stride >= width * 4 && length > 0 && length <= stride * height)
                {
                    if (_cgFrameCache.TryGetValue(mapName, out var entry) && entry.Sequence == sequence && entry.Jpeg != null)
                    {
                        return entry.Jpeg;
                    }

                    var pixels = new byte[length];
                    view.ReadArray(256, pixels, 0, pixels.Length);
                    if (view.ReadInt64(28) == sequence)
                    {
                        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
                        try
                        {
                            using var bmp = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
                            using var ms = new MemoryStream();
                            if (width > 960)
                            {
                                var scaledW = 960;
                                var scaledH = (int)Math.Round((double)height * scaledW / width);
                                using var scaledBmp = new System.Drawing.Bitmap(scaledW, scaledH);
                                using (var g = System.Drawing.Graphics.FromImage(scaledBmp))
                                {
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                                    g.DrawImage(bmp, 0, 0, scaledW, scaledH);
                                }
                                scaledBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            else
                            {
                                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            var jpeg = ms.ToArray();
                            _cgFrameCache[mapName] = (sequence, jpeg, DateTime.UtcNow);
                            return jpeg;
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                    else if (_cgFrameCache.TryGetValue(mapName, out var oldEntry) && oldEntry.Jpeg != null)
                    {
                        return oldEntry.Jpeg;
                    }
                }
            }
        }
        catch
        {
            if (_cgFrameCache.TryGetValue(mapName, out var oldEntry) && oldEntry.Jpeg != null && (DateTime.UtcNow - oldEntry.Timestamp).TotalSeconds < 4)
            {
                return oldEntry.Jpeg;
            }
        }

        return GetPlaceholderCgFrameJpeg(label);
    }

    private static byte[] GetPlaceholderCgFrameJpeg(string label)
    {
        if (_cgPlaceholderCache.TryGetValue(label, out var cached)) return cached;

        const int width = 960;
        const int height = 540;
        using var bmp = new System.Drawing.Bitmap(width, height);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.FromArgb(10, 14, 20));
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 255, 255, 255), 2);
            g.DrawRectangle(pen, 20, 20, width - 40, height - 40);

            var isProg = string.Equals(label, "PROGRAM", StringComparison.OrdinalIgnoreCase);
            var badgeColor = isProg
                ? System.Drawing.Color.FromArgb(185, 28, 28)
                : System.Drawing.Color.FromArgb(67, 56, 202);

            using var badgeBrush = new System.Drawing.SolidBrush(badgeColor);
            g.FillRectangle(badgeBrush, width / 2 - 90, height / 2 - 40, 180, 36);

            using var font = new System.Drawing.Font("Segoe UI", 13, System.Drawing.FontStyle.Bold);
            using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
            g.DrawString($"CG {label}", font, textBrush, new System.Drawing.RectangleF(width / 2 - 90, height / 2 - 40, 180, 36), sf);

            using var subFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Regular);
            using var subBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(156, 163, 175));
            g.DrawString("STANDBY · WAITING FOR ENGINE", subFont, subBrush, new System.Drawing.PointF(width / 2, height / 2 + 18), sf);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        var bytes = ms.ToArray();
        _cgPlaceholderCache[label] = bytes;
        return bytes;
    }

    private static byte[]? RenderProjectToPngBytes(CgProject project, int width, int height)
    {
        try
        {
            using var compositor = new CgCompositor();
            var timeline = ResolveProjectTimeline(project);
            var frame = compositor.RenderProjectSurface(project, width, height, timeline);
            if (frame.Bgra == null || frame.Bgra.Length == 0) return null;

            var handle = System.Runtime.InteropServices.GCHandle.Alloc(frame.Bgra, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                using var bmp = new System.Drawing.Bitmap(frame.Width, frame.Height, frame.Stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RenderProjectToPngBytes failed for '{project.Name}': {ex.Message}");
            return null;
        }
    }

    private static double ResolveProjectTimeline(CgProject project)
    {
        var visible = project.Layers?.Where(x => x.Visible).ToArray() ?? [];
        if (visible.Length == 0) return 0;
        var intro = visible.Select(x => Math.Max(0, x.AnimationInSeconds)).DefaultIfEmpty(0).Max();
        var firstStart = visible.Select(x => Math.Max(0, x.StartSeconds)).DefaultIfEmpty(0).Min();
        var duration = Math.Max(0.1, project.DurationSeconds);
        return Math.Clamp(firstStart + intro + 0.15, 0, Math.Max(0, duration - 0.1));
    }

    private static CgProject? BuildStopTarget(JsonElement root)
    {
        var projectId = ReadJsonString(root, "projectId", string.Empty).Trim();
        var name = ReadJsonString(root, "template", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = ReadJsonString(root, "name", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(projectId) && string.IsNullOrWhiteSpace(name)) return null;
        var stub = new CgProject { Name = string.IsNullOrWhiteSpace(name) ? "\u0000" : name };
        stub.Id = Guid.TryParse(projectId, out var g) ? g : Guid.Empty;
        return stub;
    }

    private static string ReadJsonString(JsonElement root, string property, string fallback)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;

    private sealed record CgTarget(string ChannelId, string? DataJson);
    private static List<CgTarget> ReadCgTargets(JsonElement root)
    {
        var result = new List<CgTarget>();
        if (!root.TryGetProperty("targets", out var targets) || targets.ValueKind != JsonValueKind.Array) return result;
        foreach (var entry in targets.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String)
            {
                var id = entry.GetString(); if (!string.IsNullOrWhiteSpace(id)) result.Add(new CgTarget(id.Trim(), null));
            }
            else if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("channelId", out var c) && c.ValueKind == JsonValueKind.String)
            {
                var id = c.GetString(); if (string.IsNullOrWhiteSpace(id)) continue;
                var data = entry.TryGetProperty("data", out var d) ? d.GetRawText() : null;
                result.Add(new CgTarget(id.Trim(), data));
            }
        }
        return result;
    }

    private static string? ReadRootDataJson(JsonElement root) => root.TryGetProperty("data", out var data) ? data.GetRawText() : null;
    private static CgProject CloneCgProject(CgProject source) => JsonSerializer.Deserialize<CgProject>(JsonSerializer.Serialize(source, Json), Json) ?? throw new InvalidOperationException("Unable to clone CG project.");
    private static void ApplyCgData(CgProject project, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            var root = doc.RootElement;

            string? fill = TryResolveJson(root, "fill", out var f) ? f : (TryResolveJson(root, "textColor", out var tc) ? tc : null);
            string? bg = TryResolveJson(root, "background", out var b) ? b : (TryResolveJson(root, "bgColor", out var bgc) ? bgc : null);
            string? fontSize = TryResolveJson(root, "fontSize", out var fs) ? fs : null;
            string? opacity = TryResolveJson(root, "opacity", out var op) ? op : null;

            foreach (var layer in project.Layers)
            {
                if (fill != null && (layer.Type.Equals("Text", StringComparison.OrdinalIgnoreCase) || layer.Type.Equals("Ticker", StringComparison.OrdinalIgnoreCase)))
                    layer.Fill = fill;
                if (bg != null && (layer.Type.Equals("Rectangle", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(layer.Background)))
                    layer.Background = bg;
                if (fontSize != null && double.TryParse(fontSize, out var fsd))
                    layer.FontSize = fsd;
                if (opacity != null && double.TryParse(opacity, out var opd))
                    layer.Opacity = Math.Clamp(opd, 0.0, 1.0);

                if (!layer.Type.Equals("Text", StringComparison.OrdinalIgnoreCase) && !layer.Type.Equals("Ticker", StringComparison.OrdinalIgnoreCase) && !layer.Type.Equals("Roll", StringComparison.OrdinalIgnoreCase)) continue;
                var key = string.IsNullOrWhiteSpace(layer.DataField) ? layer.Name : layer.DataField;
                if (TryResolveJson(root, key, out var value))
                {
                    layer.Text = value;
                }
                else if (TryResolveJson(root, "Headline", out var hl) && (layer.Name.Contains("Line 1", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Headline", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Title", StringComparison.OrdinalIgnoreCase)))
                {
                    layer.Text = hl;
                }
                else if (TryResolveJson(root, "Subtitle", out var st) && (layer.Name.Contains("Line 2", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Subtitle", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Desc", StringComparison.OrdinalIgnoreCase)))
                {
                    layer.Text = st;
                }
                else if (TryResolveJson(root, "Badge", out var bd) && (layer.Name.Contains("Badge", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Tag", StringComparison.OrdinalIgnoreCase) || layer.Name.Contains("Kicker", StringComparison.OrdinalIgnoreCase)))
                {
                    layer.Text = bd;
                }
            }
        }
        catch (JsonException) { }
    }
    private static bool TryResolveJson(JsonElement root, string? path, out string value)
    {
        value = string.Empty; if (root.ValueKind != JsonValueKind.Object || string.IsNullOrWhiteSpace(path)) return false; var current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object) return false; JsonElement next = default; var found = false;
            foreach (var p in current.EnumerateObject()) if (p.Name.Equals(segment, StringComparison.OrdinalIgnoreCase)) { next = p.Value; found = true; break; }
            if (!found) return false; current = next;
        }
        value = current.ValueKind == JsonValueKind.String ? current.GetString() ?? string.Empty : current.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False ? current.ToString() : current.GetRawText(); return true;
    }

    private async Task HandleMcp(HttpListenerContext c, CancellationToken ct)
    {
        using var d = await ReadJson(c.Request, ct);
        var root = d.RootElement;
        var id = root.TryGetProperty("id", out var idEl) ? idEl.Clone() : default;
        var method = root.TryGetProperty("method", out var m) ? m.GetString() ?? string.Empty : string.Empty;
        if (method == "tools/list")
        {
            var result = new
            {
                tools = new object[]{
                Tool("playout_get_status","Read live channel status"),
                Tool("playout_command","Execute validated playout command"),
                Tool("channel_controller_list","List connected playout channels"),
                Tool("cg_controller_play","Play an authored CG template on one or more channels"),
                Tool("cg_controller_onair","List live CG items active on PROGRAM across channels"),
                Tool("cg_controller_stop","Stop/animate-out specific CG item by project ID, template name, or layer"),
                Tool("demo_load_and_test","Load Kashtrix professional demo data and validate CG/MOS/NRCS/Playout/Prompter integration"),
                Tool("nrcs_list_rundowns","List NRCS rundowns"),
                Tool("nrcs_list_stories","List/search independent NRCS Story Bank stories"),
                Tool("nrcs_list_assignments","List newsroom planning assignments"),
                Tool("nrcs_get_live","Read the live story state for a rundown"),
                Tool("nrcs_set_live","Set/clear the live story state for a rundown"),
                Tool("nrcs_publish_rundown","Publish NRCS rundown to playout and optionally MOS/NCS"),
                Tool("prompter_get_rundown","Read a rundown and prompter story body"),
                Tool("ha_get_state","Read HA node/election state"),
                Tool("ha_force_active","Force a channel active on a selected HA node"),
                Tool("ha_release","Return a channel to automatic HA election")
            }
            };
            await WriteJson(c, new { jsonrpc = "2.0", id, result }); return;
        }
        if (method == "tools/call" && root.TryGetProperty("params", out var ps))
        {
            var name = ps.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            var args = ps.TryGetProperty("arguments", out var ar) && ar.ValueKind == JsonValueKind.Object ? ar : default;
            if (name == "playout_get_status")
            {
                var channel = ArgString(args, "channelId"); var st = PlatformControlBus.ReadStatuses().FirstOrDefault(x => x.ChannelId.Equals(channel, StringComparison.OrdinalIgnoreCase));
                await WriteMcpResult(c, id, st); return;
            }
            if (name == "playout_command")
            {
                var channel = ArgString(args, "channelId"); var action = ArgString(args, "action");
                var resp = await PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { ChannelId = channel, Action = action }, TimeSpan.FromSeconds(3), ct); await WriteMcpResult(c, id, resp); return;
            }
            if (name == "channel_controller_list") { await WriteMcpResult(c, id, PlatformControlBus.ReadStatuses()); return; }
            if (name == "channel_controller_command")
            {
                var targets = ArgStrings(args, "targets"); var action = ArgString(args, "action"); var tasks = targets.Select(ch => PlatformControlBus.SubmitAndWaitAsync(new PlatformControlCommand { ChannelId = ch, Action = action }, TimeSpan.FromSeconds(3), ct)); await WriteMcpResult(c, id, await Task.WhenAll(tasks)); return;
            }
            if (name == "cg_controller_play")
            {
                var targets = ArgStrings(args, "targets"); var template = ArgString(args, "template"); var layer = ArgInt(args, "layer", 20); var data = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("data", out var dataEl) ? dataEl.GetRawText() : "{}";
                using var fake = JsonDocument.Parse(JsonSerializer.Serialize(new { targets, template, layer, bus = "PROGRAM", data = JsonSerializer.Deserialize<object>(data, Json) }, Json)); await WriteMcpResult(c, id, HandleCgControllerCommand("play", fake)); return;
            }
            if (name == "cg_controller_onair")
            {
                var ch = ArgString(args, "channelId");
                var snaps = string.IsNullOrWhiteSpace(ch) ? CgOnAirRegistry.ReadAll() : (CgOnAirRegistry.Read(ch) is { } one ? new List<CgOnAirSnapshot> { one } : new List<CgOnAirSnapshot>());
                await WriteMcpResult(c, id, new { count = snaps.Sum(s => s.Items.Count), items = snaps.SelectMany(s => s.Items) }); return;
            }
            if (name == "cg_controller_stop")
            {
                var targets = ArgStrings(args, "targets"); var template = ArgString(args, "template"); var projId = ArgString(args, "projectId"); var layer = ArgInt(args, "layer", -1);
                using var fake = JsonDocument.Parse(JsonSerializer.Serialize(new { targets, template, projectId = projId, layer, bus = "PROGRAM" }, Json)); await WriteMcpResult(c, id, HandleCgControllerCommand("stop", fake)); return;
            }
            if (name == "demo_load_and_test") { await WriteMcpResult(c, id, new DemoIntegrationService().SeedAndValidate()); return; }
            if (name == "nrcs_list_rundowns") { await WriteMcpResult(c, id, new NrcsPlatformStore().ListRundowns()); return; }
            if (name == "nrcs_list_stories") { var q = ArgString(args, "query"); await WriteMcpResult(c, id, new NrcsPlatformStore().ListStoryBank(q)); return; }
            if (name == "nrcs_list_assignments") { await WriteMcpResult(c, id, new NrcsPlatformStore().ListAssignments()); return; }
            if (name == "nrcs_get_live") { var rid = ArgString(args, "rundownId"); await WriteMcpResult(c, id, new NrcsPlatformStore().GetLiveState(rid)); return; }
            if (name == "nrcs_set_live")
            {
                var rid = ArgString(args, "rundownId"); var sid = ArgString(args, "storyId"); var item = ArgString(args, "rundownItemId"); var onAir = ArgBool(args, "onAir", true); var store = new NrcsPlatformStore(); store.SetLiveStory(rid, sid, item, onAir); await WriteMcpResult(c, id, store.GetLiveState(rid)); return;
            }
            if (name == "nrcs_publish_rundown")
            {
                var rid = ArgString(args, "rundownId"); var publishMos = ArgBool(args, "publishMos", false); var store = new NrcsPlatformStore(); var count = store.PublishToPlayout(rid); object? mos = null; if (publishMos) { var r = await store.PublishMosAsync(rid, ct); mos = new { r.Path, r.Sent, r.Message }; }
                await WriteMcpResult(c, id, new { rundownId = rid, playoutItems = count, mos }); return;
            }
            if (name == "prompter_get_rundown")
            {
                var rid = ArgString(args, "rundownId"); var store = new NrcsPlatformStore(); await WriteMcpResult(c, id, new { rundown = store.ListRundowns().FirstOrDefault(x => x.Id == rid), stories = store.ListStories(rid) }); return;
            }
            if (name == "ha_get_state") { await WriteMcpResult(c, id, HaControlBus.ReadSnapshot()); return; }
            if (name == "ha_force_active" || name == "ha_release")
            {
                var channelId = ArgString(args, "channelId"); var nodeId = ArgString(args, "nodeId"); var duration = ArgInt(args, "durationSeconds", 600); var response = await HaControlBus.SubmitAndWaitAsync(new HaControlCommand { Action = name == "ha_force_active" ? "force_active" : "release", ChannelId = channelId, NodeId = nodeId, DurationSeconds = duration, Reason = "MCP" }, TimeSpan.FromSeconds(3), ct); await WriteMcpResult(c, id, response); return;
            }
        }
        await WriteJson(c, new { jsonrpc = "2.0", id, error = new { code = -32601, message = "Method/tool not found" } });
    }

    private static string ArgString(JsonElement args, string name) => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    private static int ArgInt(JsonElement args, string name, int fallback) => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.TryGetInt32(out var n) ? n : fallback;
    private static bool ArgBool(JsonElement args, string name, bool fallback) => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : fallback;
    private static string[] ArgStrings(JsonElement args, string name) => args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() : Array.Empty<string>();
    private static Task WriteMcpResult(HttpListenerContext c, JsonElement id, object? value) => WriteJson(c, new { jsonrpc = "2.0", id, result = new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(value, Json) } } } });
    private static object Tool(string name, string description) => new { name, description, inputSchema = new { type = "object", additionalProperties = true } };
    private static DateTime? ReadNullableUtc(JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        if (value.ValueKind != JsonValueKind.String) return null;
        var text = value.GetString();
        return DateTime.TryParse(text, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed) ? parsed.ToUniversalTime() : null;
    }
    private static async Task<JsonDocument> ReadJson(HttpListenerRequest r, CancellationToken ct)
    {
        // Windows PowerShell 5.1 can send a .NET string body using the process ANSI code page
        // unless the caller explicitly supplies UTF-8 bytes. JsonDocument's stream overload
        // expects UTF-8 JSON and an invalid byte sequence can surface as the otherwise opaque
        // "Cannot transcode invalid UTF-8 JSON text to UTF-16 string" failure. Read the request
        // once, then normalize it to valid UTF-8 JSON. This also keeps third-party MOS/NRCS
        // clients that correctly declare another charset interoperable with the REST gateway.
        using var buffer = new MemoryStream();
        await r.InputStream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        var bytes = buffer.ToArray();
        if (bytes.Length == 0) return JsonDocument.Parse("{}");
        try { return JsonDocument.Parse(bytes); }
        catch (JsonException first)
        {
            Encoding encoding;
            try { encoding = r.ContentEncoding ?? Encoding.UTF8; }
            catch { encoding = Encoding.UTF8; }
            try
            {
                var text = encoding.GetString(bytes);
                return JsonDocument.Parse(text);
            }
            catch
            {
                // Last recovery path: replacement fallback guarantees a valid UTF-16 string,
                // while malformed JSON still reports INVALID_REQUEST instead of crashing HTTP.
                var safeUtf8 = new UTF8Encoding(false, false).GetString(bytes);
                try { return JsonDocument.Parse(safeUtf8); }
                catch { throw new JsonException("Request body is not valid JSON/UTF-8.", first); }
            }
        }
    }
    private static async Task WriteJson(HttpListenerContext c, object value) { var b = JsonSerializer.SerializeToUtf8Bytes(value, Json); c.Response.ContentType = "application/json; charset=utf-8"; c.Response.ContentLength64 = b.Length; await c.Response.OutputStream.WriteAsync(b); c.Response.Close(); }
    private static async Task WriteHtml(HttpListenerContext c, string value) { var b = Encoding.UTF8.GetBytes(value); c.Response.ContentType = "text/html; charset=utf-8"; c.Response.ContentLength64 = b.Length; await c.Response.OutputStream.WriteAsync(b); c.Response.Close(); }
    private string DashboardHtml() => """<!doctype html><html><head><meta charset='utf-8'><title>Kashtrix MCR</title><style>body{font-family:Segoe UI;background:#070a10;color:#eee;margin:30px}h1{color:#d86ac1}.card{border:1px solid #3e2854;background:#10151d;padding:16px;margin:10px 0;border-radius:8px}code{color:#dcb9ff}</style></head><body><h1>Kashtrix Web MCR / API Gateway</h1><p>REST <code>/api/v1</code> · WebSocket <code>/api/v1/events/ws</code> · MCP <code>/mcp</code></p><div id='x'></div><script>async function tick(){let r=await fetch('/api/v1/health');let h=await r.json();document.getElementById('x').innerHTML='<div class=card>Gateway OK · UTC '+h.utc+' · channels '+h.channels+'</div>';}tick();setInterval(tick,2000);</script></body></html>""";
    private static async Task ServeMediaFileAsync(HttpListenerContext c, CancellationToken ct)
    {
        try
        {
            var rawPath = c.Request.QueryString["path"];
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                c.Response.StatusCode = 400;
                c.Response.Close();
                return;
            }

            var filePath = Uri.UnescapeDataString(rawPath).Trim('"');
            if (!File.Exists(filePath))
            {
                c.Response.StatusCode = 404;
                c.Response.Close();
                return;
            }

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".mp4" or ".m4v" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".mkv" => "video/x-matroska",
                ".avi" => "video/x-msvideo",
                ".ts" or ".m2ts" => "video/mp2t",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".aac" => "audio/aac",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileInfo = new FileInfo(filePath);
            var totalBytes = fileInfo.Length;

            c.Response.ContentType = contentType;
            c.Response.AddHeader("Accept-Ranges", "bytes");

            var rangeHeader = c.Request.Headers["Range"];
            if (!string.IsNullOrWhiteSpace(rangeHeader) && rangeHeader.StartsWith("bytes="))
            {
                var rangeVal = rangeHeader["bytes=".Length..].Trim();
                var parts = rangeVal.Split('-');
                long start = 0;
                long end = totalBytes - 1;

                if (!string.IsNullOrWhiteSpace(parts[0]) && long.TryParse(parts[0], out var p0))
                {
                    start = Math.Clamp(p0, 0, totalBytes - 1);
                }

                if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) && long.TryParse(parts[1], out var p1))
                {
                    end = Math.Clamp(p1, start, totalBytes - 1);
                }

                var chunkLen = (end - start) + 1;
                c.Response.StatusCode = 206; // Partial Content
                c.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{totalBytes}");
                c.Response.ContentLength64 = chunkLen;

                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(start, SeekOrigin.Begin);

                var buffer = new byte[65536];
                var bytesRemaining = chunkLen;
                while (bytesRemaining > 0 && !ct.IsCancellationRequested)
                {
                    var toRead = (int)Math.Min(buffer.Length, bytesRemaining);
                    var read = await fs.ReadAsync(buffer.AsMemory(0, toRead), ct);
                    if (read == 0) break;
                    await c.Response.OutputStream.WriteAsync(buffer.AsMemory(0, read), ct);
                    bytesRemaining -= read;
                }
            }
            else
            {
                c.Response.StatusCode = 200;
                c.Response.ContentLength64 = totalBytes;

                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await fs.CopyToAsync(c.Response.OutputStream, 65536, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (HttpListenerException) { }
        catch (IOException) { }
        catch (Exception)
        {
            try { c.Response.StatusCode = 500; } catch { }
        }
        finally
        {
            try { c.Response.Close(); } catch { }
        }
    }

    private void Write(string text) => Log?.Invoke($"{DateTime.Now:HH:mm:ss}  {text}");
    private static GatewaySettings LoadSettings() { try { Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!); if (File.Exists(SettingsPath)) return JsonSerializer.Deserialize<GatewaySettings>(File.ReadAllText(SettingsPath), Json) ?? new(); var s = new GatewaySettings(); File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true })); return s; } catch { return new(); } }
    public void Dispose() => Stop();
}

public static class GatewayPlayoutEngine
{
    private static readonly object _lock = new();
    private static readonly PlatformChannelStatus _status = new()
    {
        ChannelId = "KTX-PLAYOUT-01",
        ChannelName = "Kashtrix Playout Main",
        Engine = "IDLE",
        CurrentItemId = "",
        CurrentTitle = "",
        SelectedItemId = "",
        SelectedTitle = "",
        ElapsedSeconds = 0,
        DurationSeconds = 0,
        OutputArmed = true,
        HaStatus = "STANDALONE",
        CapabilityStatus = "SDI · NDI · 1080p50",
        UpdatedUtc = DateTime.UtcNow
    };

    private static readonly System.Threading.Timer _ticker;
    private static DateTime _lastTick = DateTime.UtcNow;

    static GatewayPlayoutEngine()
    {
        EnsureInitialized();
        _ticker = new System.Threading.Timer(_ => Tick(), null, 500, 500);
    }

    public static void EnsureInitialized()
    {
        lock (_lock)
        {
            var db = new BroadcastDatabase();
            var playlist = db.LoadPlaylist();
            if (playlist.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(_status.SelectedItemId))
                {
                    _status.SelectedItemId = playlist[0].Id.ToString("D");
                    _status.SelectedTitle = playlist[0].Title;
                }
            }
            PlatformControlBus.PublishStatus(_status);
        }
    }

    public static PlatformChannelStatus GetStatus(string channelId)
    {
        lock (_lock)
        {
            return new PlatformChannelStatus
            {
                ChannelId = _status.ChannelId,
                ChannelName = _status.ChannelName,
                Engine = _status.Engine,
                CurrentItemId = _status.CurrentItemId,
                CurrentTitle = _status.CurrentTitle,
                SelectedItemId = _status.SelectedItemId,
                SelectedTitle = _status.SelectedTitle,
                ElapsedSeconds = Math.Round(_status.ElapsedSeconds, 2),
                DurationSeconds = _status.DurationSeconds,
                OutputArmed = _status.OutputArmed,
                HaStatus = _status.HaStatus,
                CapabilityStatus = _status.CapabilityStatus,
                UpdatedUtc = _status.UpdatedUtc
            };
        }
    }

    private static void Tick()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var delta = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            if (_status.Engine == "PLAYING" && !string.IsNullOrWhiteSpace(_status.CurrentItemId))
            {
                _status.ElapsedSeconds += delta;
                if (_status.DurationSeconds > 0 && _status.ElapsedSeconds >= _status.DurationSeconds)
                {
                    TakeNextInternal();
                }
                PlatformControlBus.PublishStatus(_status);
            }
        }
    }

    public static PlatformControlResponse Execute(string channelId, string action, Dictionary<string, string> parameters, string id)
    {
        lock (_lock)
        {
            var act = (action ?? "").Trim().ToLowerInvariant();
            var db = new BroadcastDatabase();
            var playlist = db.LoadPlaylist();

            if (act == "take")
            {
                string targetId = parameters.TryGetValue("itemId", out var itm) ? itm : _status.SelectedItemId;
                var targetItem = playlist.FirstOrDefault(x => x.Id.ToString("D").Equals(targetId, StringComparison.OrdinalIgnoreCase))
                    ?? playlist.FirstOrDefault(x => x.Title.Equals(targetId, StringComparison.OrdinalIgnoreCase))
                    ?? playlist.FirstOrDefault();

                if (targetItem != null)
                {
                    _status.CurrentItemId = targetItem.Id.ToString("D");
                    _status.CurrentTitle = targetItem.Title;
                    _status.DurationSeconds = targetItem.Duration.TotalSeconds > 0 ? targetItem.Duration.TotalSeconds : 30.0;
                    _status.ElapsedSeconds = 0;
                    _status.Engine = "PLAYING";

                    var currentIdx = playlist.IndexOf(targetItem);
                    if (currentIdx >= 0 && currentIdx + 1 < playlist.Count)
                    {
                        _status.SelectedItemId = playlist[currentIdx + 1].Id.ToString("D");
                        _status.SelectedTitle = playlist[currentIdx + 1].Title;
                    }
                }
            }
            else if (act == "next")
            {
                TakeNextInternal();
            }
            else if (act == "play" || act == "resume")
            {
                if (string.IsNullOrWhiteSpace(_status.CurrentItemId) && playlist.Count > 0)
                {
                    var item = playlist[0];
                    _status.CurrentItemId = item.Id.ToString("D");
                    _status.CurrentTitle = item.Title;
                    _status.DurationSeconds = item.Duration.TotalSeconds > 0 ? item.Duration.TotalSeconds : 30.0;
                    _status.ElapsedSeconds = 0;
                }
                _status.Engine = "PLAYING";
            }
            else if (act == "pause")
            {
                _status.Engine = _status.Engine == "PLAYING" ? "PAUSED" : "PLAYING";
            }
            else if (act == "stop")
            {
                _status.Engine = "IDLE";
                _status.ElapsedSeconds = 0;
            }
            else if (act == "cue")
            {
                if (parameters.TryGetValue("itemId", out var cueId))
                {
                    var itm = playlist.FirstOrDefault(x => x.Id.ToString("D").Equals(cueId, StringComparison.OrdinalIgnoreCase))
                           ?? playlist.FirstOrDefault(x => x.Title.Equals(cueId, StringComparison.OrdinalIgnoreCase));
                    if (itm != null)
                    {
                        _status.SelectedItemId = itm.Id.ToString("D");
                        _status.SelectedTitle = itm.Title;
                    }
                }
            }
            else if (act == "seek")
            {
                if (parameters.TryGetValue("offsetSeconds", out var secStr) && double.TryParse(secStr, out var sec))
                {
                    _status.ElapsedSeconds = Math.Clamp(sec, 0, _status.DurationSeconds);
                }
            }
            else if (act == "output_arm")
            {
                if (parameters.TryGetValue("armed", out var armStr) && bool.TryParse(armStr, out var armed))
                {
                    _status.OutputArmed = armed;
                }
            }

            _status.UpdatedUtc = DateTime.UtcNow;
            PlatformControlBus.PublishStatus(_status);

            return new PlatformControlResponse
            {
                Id = id,
                Ok = true,
                Code = "OK",
                Message = $"Command '{action}' handled by channel controller."
            };
        }
    }

    private static void TakeNextInternal()
    {
        var db = new BroadcastDatabase();
        var playlist = db.LoadPlaylist();
        if (playlist.Count == 0) return;

        int currentIdx = -1;
        if (!string.IsNullOrWhiteSpace(_status.CurrentItemId))
        {
            currentIdx = playlist.FindIndex(x => x.Id.ToString("D").Equals(_status.CurrentItemId, StringComparison.OrdinalIgnoreCase));
        }

        int nextIdx = (currentIdx + 1) % playlist.Count;
        var nextItem = playlist[nextIdx];

        _status.CurrentItemId = nextItem.Id.ToString("D");
        _status.CurrentTitle = nextItem.Title;
        _status.DurationSeconds = nextItem.Duration.TotalSeconds > 0 ? nextItem.Duration.TotalSeconds : 30.0;
        _status.ElapsedSeconds = 0;
        _status.Engine = "PLAYING";

        int cuedIdx = (nextIdx + 1) % playlist.Count;
        _status.SelectedItemId = playlist[cuedIdx].Id.ToString("D");
        _status.SelectedTitle = playlist[cuedIdx].Title;
    }
}
