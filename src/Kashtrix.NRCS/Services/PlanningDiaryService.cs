using System;
using System.Collections.Generic;
using System.Linq;

namespace Kashtrix.NRCS.Services;

public sealed record PlanningEvent(
    string Id,
    string Title,
    string Category,
    string Location,
    DateTime EventDateUtc,
    string Contacts,
    string Description,
    string Status);

public sealed class PlanningDiaryService
{
    private static readonly Lazy<PlanningDiaryService> _instance = new(() => new PlanningDiaryService());
    public static PlanningDiaryService Instance => _instance.Value;

    private readonly List<PlanningEvent> _events = new();

    public PlanningDiaryService()
    {
        SeedSampleDiary();
    }

    private void SeedSampleDiary()
    {
        var today = DateTime.UtcNow.Date;
        _events.Add(new PlanningEvent(
            "EVT-01",
            "MUNICIPAL INFRASTRUCTURE & TRANSPORT BRIEFING",
            "GOVERNMENT",
            "CITY HALL · PRESS AUDITORIUM",
            today.AddHours(14),
            "Govt Spokesperson: +977-1-4200000",
            "Briefing on multi-corridor transit upgrades, ambulance right-of-way lanes, and emergency dispatch.",
            "CONFIRMED"));

        _events.Add(new PlanningEvent(
            "EVT-02",
            "CENTRAL BANK QUARTERLY MONETARY REVIEW",
            "FINANCE",
            "CENTRAL BANK AUDITORIUM",
            today.AddHours(16),
            "Media Relations: +977-1-4411223",
            "Quarterly address by Deputy Governor regarding liquidity, inflation trends, and foreign reserves.",
            "CONFIRMED"));

        _events.Add(new PlanningEvent(
            "EVT-03",
            "NATIONAL CRICKET TOURNAMENT FINAL PRACTICE",
            "SPORTS",
            "TRIBHUVAN INT'L CRICKET GROUND",
            today.AddDays(1).AddHours(10),
            "Match Manager: sports@cricket.org",
            "Final open training session before championship weekend match.",
            "SCHEDULED"));

        _events.Add(new PlanningEvent(
            "EVT-04",
            "SUPREME COURT BENCH HEARING ON AMBIENT AIR POLICY",
            "COURT",
            "COURT BENCH 3",
            today.AddDays(2).AddHours(11),
            "Court Registrar Office",
            "Constitutional bench hears environmental protection directives.",
            "PLANNED"));
    }

    public IReadOnlyList<PlanningEvent> ListEvents(string? filter = null)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return _events.OrderBy(x => x.EventDateUtc).ToList();

        var f = filter.Trim();
        return _events
            .Where(x => x.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                        x.Category.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                        x.Location.Contains(f, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.EventDateUtc)
            .ToList();
    }

    public void AddEvent(string title, string category, string location, DateTime dateUtc, string contacts, string description)
    {
        _events.Add(new PlanningEvent(
            $"EVT-{Guid.NewGuid():N}"[..6].ToUpperInvariant(),
            title.ToUpperInvariant(),
            category.ToUpperInvariant(),
            location,
            dateUtc,
            contacts,
            description,
            "PLANNED"));
    }
}
