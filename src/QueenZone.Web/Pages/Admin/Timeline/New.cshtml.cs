using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed class NewModel : AdminTimelinePageModel
{
    public TimelineFormViewModel Form { get; private set; } = BuildForm(CreateDefaultDraft(), null);

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "Add timeline event";
        Breadcrumbs = AdminBreadcrumbs.Page("Timeline", "/admin/timeline", "Add event");
    }

    public static TimelineFormViewModel BuildForm(AdminQueenHistoryDraft draft, IReadOnlyList<string>? errors) =>
        new("Add timeline event", "/admin/timeline", draft, errors);

    internal static AdminQueenHistoryDraft CreateDefaultDraft() =>
        new(
            string.Empty,
            string.Empty,
            DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Other,
            50,
            null,
            true);
}
