using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.PhotoSubmissions;

public sealed class ActionModel(IPhotoSubmissionRepository photoSubmissionRepository) : AdminPhotoSubmissionsPageModel
{
    [BindProperty]
    public string? ApprovedCategory { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            id,
            PhotoSubmissionStatus.Approved,
            rejectionReason: null,
            approvedCategory: ApprovedCategory,
            successMessage: "Photo approved.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            TempData["PhotoSubmissionMessage"] = "A rejection reason is required.";
            TempData["PhotoSubmissionMessageKind"] = "error";
            return Redirect($"/admin/photo-submissions/{id}");
        }

        return await ApplyAsync(
            id,
            PhotoSubmissionStatus.Rejected,
            rejectionReason: RejectionReason,
            approvedCategory: null,
            successMessage: "Photo rejected.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostNeedsInfoAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ReviewNotes))
        {
            TempData["PhotoSubmissionMessage"] = "A note is required when requesting changes.";
            TempData["PhotoSubmissionMessageKind"] = "error";
            return Redirect($"/admin/photo-submissions/{id}");
        }

        return await ApplyAsync(
            id,
            PhotoSubmissionStatus.NeedsInfo,
            rejectionReason: null,
            approvedCategory: null,
            successMessage: "Marked as needs info.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostUnderReviewAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyAsync(
            id,
            PhotoSubmissionStatus.UnderReview,
            rejectionReason: null,
            approvedCategory: null,
            successMessage: "Marked under review.",
            cancellationToken);
    }

    private async Task<IActionResult> ApplyAsync(
        Guid id,
        string status,
        string? rejectionReason,
        string? approvedCategory,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await photoSubmissionRepository.UpdateStatusAsync(
                id,
                status,
                EditorEmail,
                ReviewNotes,
                rejectionReason,
                approvedCategory,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["PhotoSubmissionMessage"] = successMessage;
            TempData["PhotoSubmissionMessageKind"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PhotoSubmissionMessage"] = ex.Message;
            TempData["PhotoSubmissionMessageKind"] = "error";
        }

        return Redirect($"/admin/photo-submissions/{id}");
    }
}
