using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FanPerformanceSubmissions;

public sealed class ActionModel(
    IFanPerformanceSubmissionRepository fanPerformanceSubmissionRepository,
    FanPerformanceSubmissionPromotionService fanPerformanceSubmissionPromotionService,
    AdminFanPerformanceWriteService adminFanPerformanceWriteService)
    : AdminFanPerformanceSubmissionsPageModel
{
    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public string? PerformedBy { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public string? CoveredSong { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await fanPerformanceSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null)
        {
            return NotFound();
        }

        try
        {
            var stageId = await fanPerformanceSubmissionPromotionService.PromoteAsync(
                submission,
                EditorEmail,
                ReviewNotes,
                new FanPerformanceReviewEdits(Title, PerformedBy, Description, CoveredSong),
                cancellationToken);
            await adminFanPerformanceWriteService.SyncPublishedAsync(stageId, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithMessage(id, ex.Message, "error");
        }

        return RedirectWithMessage(id, "Fan performance approved and published.", "success");
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            return RedirectWithMessage(id, "A rejection reason is required.", "error");
        }

        return await ApplyAsync(
            id,
            FanPerformanceSubmissionStatus.Rejected,
            rejectionReason: RejectionReason,
            successMessage: "Fan performance rejected.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostNeedsInfoAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ReviewNotes))
        {
            return RedirectWithMessage(id, "A note is required when requesting changes.", "error");
        }

        return await ApplyAsync(
            id,
            FanPerformanceSubmissionStatus.NeedsInfo,
            rejectionReason: null,
            successMessage: "Marked as needs info.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostUnderReviewAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            id,
            FanPerformanceSubmissionStatus.UnderReview,
            rejectionReason: null,
            successMessage: "Marked under review.",
            cancellationToken);
    }

    private async Task<IActionResult> ApplyAsync(
        Guid id,
        string status,
        string? rejectionReason,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await fanPerformanceSubmissionRepository.UpdateStatusAsync(
                id,
                status,
                EditorEmail,
                ReviewNotes,
                rejectionReason,
                cancellationToken: cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            return RedirectWithMessage(id, successMessage, "success");
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithMessage(id, ex.Message, "error");
        }
    }

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["FanPerformanceSubmissionMessage"] = message;
        TempData["FanPerformanceSubmissionMessageKind"] = kind;
        return Redirect($"/admin/fan-performance-submissions/{id}");
    }
}
