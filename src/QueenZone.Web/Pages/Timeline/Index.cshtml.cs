using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Timeline;

public sealed class IndexModel(IQueenHistoryRepository historyRepository) : PageModel
{
    public IReadOnlyList<TimelineDecadeGroup> Decades { get; private set; } = [];
    public int TotalEventCount { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } =
    [
        BreadcrumbItem.Home,
        new BreadcrumbItem("Timeline", "/timeline"),
    ];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var events = await historyRepository.GetAllPublishedAsync(cancellationToken);

        var rows = events
            .OrderBy(e => e.EventDate)
            .ThenByDescending(e => e.Importance)
            .Select(e => new TimelineEventRow(e))
            .ToList();

        TotalEventCount = rows.Count;

        Decades = rows
            .GroupBy(r => r.Decade)
            .OrderBy(g => g.Key)
            .Select(g => new TimelineDecadeGroup(
                g.Key,
                g.Select(r => r.Year).Distinct().Order().ToList(),
                g.ToList()))
            .ToList();

        ViewData["Title"] = "Queen History Timeline · Queenzone";
        ViewData["CanonicalPath"] = "/timeline";
        ViewData["Description"] = "Five decades of Queen history — concerts, releases, milestones and more, from the Queenzone archive.";
    }
}

public sealed class TimelineEventRow(QueenHistoryEvent e)
{
    public QueenHistoryEvent Event { get; } = e;
    public string Year { get; } = e.EventDate.Year.ToString();
    public string Decade { get; } = e.EventDate.Year.ToString()[..3] + "0s";
    public string DisplayCategory { get; } = e.Category.ToTimelineCategory();
    public string DisplayLabel { get; } = e.Category.ToTimelineCategoryLabel();
}

public sealed record TimelineDecadeGroup(
    string Decade,
    IReadOnlyList<string> Years,
    IReadOnlyList<TimelineEventRow> Events);

internal static class QueenHistoryEventCategoryTimelineExtensions
{
    internal static string ToTimelineCategory(this QueenHistoryEventCategory cat) => cat switch
    {
        QueenHistoryEventCategory.Concert => "live",
        QueenHistoryEventCategory.Release or QueenHistoryEventCategory.Recording => "music",
        QueenHistoryEventCategory.Award or QueenHistoryEventCategory.Birthday or QueenHistoryEventCategory.SiteHistory => "milestone",
        _ => "other",
    };

    internal static string ToTimelineCategoryLabel(this QueenHistoryEventCategory cat) => cat switch
    {
        QueenHistoryEventCategory.Concert => "Live",
        QueenHistoryEventCategory.Release => "Release",
        QueenHistoryEventCategory.Recording => "Recording",
        QueenHistoryEventCategory.Award => "Award",
        QueenHistoryEventCategory.Birthday => "Birthday",
        QueenHistoryEventCategory.TVRadio => "TV / Radio",
        QueenHistoryEventCategory.SiteHistory => "Archive",
        _ => "Other",
    };
}
