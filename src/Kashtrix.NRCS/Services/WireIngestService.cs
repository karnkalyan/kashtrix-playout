using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kashtrix.NRCS.Services;

public sealed class WireIngestService : IWireProvider
{
    private static readonly Lazy<WireIngestService> _instance = new(() => new WireIngestService());
    public static WireIngestService Instance => _instance.Value;

    private readonly List<WireArticle> _articles = new();

    public string Id => "wire-service-hub";
    public string Name => "Broadcast Multi-Agency Wire Feed";
    public string Protocol => "RSS / XML / JSON News Ingest";
    public ConnectorCapabilities Capabilities => ConnectorCapabilities.WireFeed;
    public ConnectorStatus Status { get; private set; } = ConnectorStatus.Connected;
    public string StatusMessage { get; private set; } = "Feeds active (AP, Reuters, National Desk)";

    public WireIngestService()
    {
        SeedSampleWires();
    }

    private void SeedSampleWires()
    {
        var now = DateTime.UtcNow;
        _articles.Add(new WireArticle(
            "WIR-001",
            "REUTERS",
            "CENTRAL BANK ANNOUNCES POLICY RATE STABILITY AMID ECONOMIC RESILIENCE",
            "The central banking committee voted unanimously to maintain benchmark lending rates at existing levels during today's monetary policy review. Governors emphasized that inflation continues to trend within expected bands while industrial output indices demonstrated sustained recovery in key sectors.",
            now.AddMinutes(-12),
            "URGENT",
            "BUSINESS",
            "en"));

        _articles.Add(new WireArticle(
            "WIR-002",
            "ASSOCIATED PRESS",
            "REGIONAL DIPLOMATIC SUMMIT CONCLUDES WITH CROSS-BORDER INFRASTRUCTURE ACCORD",
            "Delegations from participating regional economies concluded bilateral negotiations this afternoon with the formal signing of a multi-year transport and digital trade framework. The pact is slated to ease customs processing and establish joint emergency dispatch logistics along primary highway routes.",
            now.AddMinutes(-34),
            "NORMAL",
            "INTERNATIONAL",
            "en"));

        _articles.Add(new WireArticle(
            "WIR-003",
            "NATIONAL DESK",
            "काठमाडौं उपत्यकाका मुख्य सडक खण्डहरूमा ट्राफिक व्यवस्थापन सुधार योजना लागू",
            "काठमाडौं उपत्यका ट्राफिक प्रहरी कार्यालयले व्यस्त समयमा हुने सवारी चाप न्यूनीकरण गर्न आजदेखि नयाँ रुट र लेन अनुशासन प्रणाली सुरु गरेको छ। सार्वजनिक यातायातका साधनहरूलाई छुट्टै लेन तोकिएको र अत्यावश्यक सेवालाई प्राथमिकता दिइएको अधिकारीहरूले बताएका छन्।",
            now.AddMinutes(-58),
            "URGENT",
            "NATIONAL",
            "ne"));

        _articles.Add(new WireArticle(
            "WIR-004",
            "METEOROLOGICAL BUREAU",
            "WEATHER WARNING: HIGHER ELEVATIONS TO EXPERIENCE RAINFALL AND FOG OVERNIGHT",
            "The national weather bureau has released an advisory regarding sudden fog accumulation and isolated convective rainfall in the central valley and hilly sectors over the next 24 hours. Drivers are urged to exercise caution.",
            now.AddMinutes(-85),
            "NORMAL",
            "WEATHER",
            "en"));

        _articles.Add(new WireArticle(
            "WIR-005",
            "SPORTS WIRE",
            "CHAMPIONSHIP SEMI-FINAL TICKETS SELL OUT AHEAD OF WEEKEND CLASH",
            "Spectator demand surged as final ticket allocations for the national club championship semi-final were snapped up within forty minutes of release. Organizers have coordinated with city transit to operate dedicated shuttle buses from central hubs.",
            now.AddMinutes(-110),
            "LOW",
            "SPORTS",
            "en"));
    }

    public Task<bool> ConnectAsync(CancellationToken ct = default) => Task.FromResult(true);
    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> HealthCheckAsync(CancellationToken ct = default) => Task.FromResult(true);

    public Task<IReadOnlyList<WireArticle>> FetchWiresAsync(string? query = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<WireArticle>>(_articles.OrderByDescending(x => x.PublishedUtc).ToList());
        }

        var q = query.Trim();
        var matches = _articles
            .Where(x => x.Headline.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Body.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Agency.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        x.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.PublishedUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<WireArticle>>(matches);
    }

    public void AddCustomWire(string agency, string headline, string body, string urgency, string category, string language = "en")
    {
        _articles.Insert(0, new WireArticle(
            $"WIR-{Guid.NewGuid():N}"[..7].ToUpperInvariant(),
            agency.ToUpperInvariant(),
            headline.ToUpperInvariant(),
            body,
            DateTime.UtcNow,
            urgency,
            category,
            language));
    }
}
