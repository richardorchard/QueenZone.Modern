using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.MessageReports;

public sealed class ActionModel(IPrivateMessageReportReviewRepository reportReviewRepository)
    : AdminMessageReportsPageModel
{
    [BindProperty]
    public string? Status { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Status))
        {
            return RedirectWithMessage(id, "A status is required.", "error");
        }

        try
        {
            var updated = await reportReviewRepository.UpdateReportStatusAsync(
                id,
                Status,
                EditorEmail,
                ReviewNotes,
                cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            TempData["MessageReportMessage"] = $"Marked as {PrivateMessageReportStatus.DisplayName(updated.Status)}.";
            TempData["MessageReportMessageKind"] = "success";
        }
        catch (ArgumentException ex)
        {
            TempData["MessageReportMessage"] = ex.Message;
            TempData["MessageReportMessageKind"] = "error";
        }

        return Redirect($"/admin/message-reports/{id}");
    }

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["MessageReportMessage"] = message;
        TempData["MessageReportMessageKind"] = kind;
        return Redirect($"/admin/message-reports/{id}");
    }
}
