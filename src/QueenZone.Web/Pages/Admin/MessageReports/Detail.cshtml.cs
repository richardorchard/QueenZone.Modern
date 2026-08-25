using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.MessageReports;

public sealed class DetailModel(IPrivateMessageReportReviewRepository reportReviewRepository)
    : AdminMessageReportsPageModel
{
    public PrivateMessageReportReviewContext? Review { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var recorded = await reportReviewRepository.RecordAccessAsync(
            id,
            PrivateMessageReportAuditActions.Viewed,
            EditorEmail,
            details: null,
            cancellationToken);
        if (!recorded)
        {
            return NotFound();
        }

        Review = await reportReviewRepository.GetReportedMessageContextAsync(id, cancellationToken);
        if (Review is null)
        {
            return NotFound();
        }

        StatusMessage = TempData["MessageReportMessage"] as string;
        StatusMessageKind = TempData["MessageReportMessageKind"] as string;
        ViewData["Title"] = $"Message report — {Review.ReportedDisplayName}";
        return Page();
    }
}
