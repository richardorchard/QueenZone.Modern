using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.PhotoSubmissions;

public sealed class ActionModel(
    IPhotoSubmissionRepository photoSubmissionRepository,
    PhotoSubmissionPromotionService photoSubmissionPromotionService,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore) : AdminPhotoSubmissionsPageModel
{
    [BindProperty]
    public string? ApprovedCategory { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    public async Task<IActionResult> OnPostApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await photoSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null)
        {
            return NotFound();
        }

        var category = ApprovedCategory ?? submission.SuggestedCategory;
        if (string.IsNullOrWhiteSpace(category))
        {
            return RedirectWithMessage(id, "A gallery category is required.", "error");
        }

        try
        {
            await photoSubmissionPromotionService.PromoteAsync(
                submission,
                category,
                EditorEmail,
                ReviewNotes,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithMessage(id, ex.Message, "error");
        }

        await QueenZone.Web.Pages.Admin.Photos.ActionModel.InvalidatePublicPhotoCachesAsync(
            publicQueryCache, coreSitemapService, outputCacheStore, cancellationToken);

        return RedirectWithMessage(id, "Photo approved and published to the gallery.", "success");
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

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["PhotoSubmissionMessage"] = message;
        TempData["PhotoSubmissionMessageKind"] = kind;
        return Redirect($"/admin/photo-submissions/{id}");
    }
}
