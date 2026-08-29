using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed record TimelineFormViewModel(
    string Title,
    string Action,
    AdminQueenHistoryDraft Draft,
    IReadOnlyList<string>? Errors,
    QueenHistoryEvent? Event = null);
