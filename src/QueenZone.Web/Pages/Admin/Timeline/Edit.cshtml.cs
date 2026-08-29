using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed class EditModel(IAdminQueenHistoryRepository historyRepository) : AdminTimelinePageModel
{
    public TimelineFormViewModel? Form { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var historyEvent = await historyRepository.GetByIdAsync(id, cancellationToken);
        if (historyEvent is null)
        {
            return NotFound();
        }

        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Edit timeline event";
        Form = BuildForm(historyEvent, ToDraft(historyEvent), null);
        return Page();
    }

    public static TimelineFormViewModel BuildForm(
        QueenHistoryEvent historyEvent,
        AdminQueenHistoryDraft draft,
        IReadOnlyList<string>? errors) =>
        new(
            "Edit timeline event",
            $"/admin/timeline/{historyEvent.Id}",
            draft,
            errors,
            historyEvent);
}
