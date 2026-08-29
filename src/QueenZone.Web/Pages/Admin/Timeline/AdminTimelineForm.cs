using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed class AdminTimelineForm
{
    [FromForm(Name = "title")]
    public string Title { get; init; } = string.Empty;

    [FromForm(Name = "summary")]
    public string Summary { get; init; } = string.Empty;

    [FromForm(Name = "eventDate")]
    public string EventDate { get; init; } = string.Empty;

    [FromForm(Name = "datePrecision")]
    public QueenHistoryDatePrecision DatePrecision { get; init; }

    [FromForm(Name = "category")]
    public QueenHistoryEventCategory Category { get; init; }

    [FromForm(Name = "importance")]
    public int Importance { get; init; }

    [FromForm(Name = "sourceUrl")]
    public string? SourceUrl { get; init; }

    [FromForm(Name = "isPublished")]
    public bool IsPublished { get; init; }

    public AdminQueenHistoryDraft ToDraft()
    {
        DateTime eventDate = default;
        if (DateTime.TryParseExact(
                EventDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            eventDate = new DateTime(parsed.Year, parsed.Month, parsed.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        return new AdminQueenHistoryDraft(
            (Title ?? string.Empty).Trim(),
            (Summary ?? string.Empty).Trim(),
            eventDate,
            DatePrecision,
            Category,
            Importance,
            string.IsNullOrWhiteSpace(SourceUrl) ? null : SourceUrl.Trim(),
            IsPublished);
    }
}
