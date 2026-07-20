using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsSuggestions;

public sealed class ActionModel(
    INewsSuggestionRepository newsSuggestionRepository,
    INewsDiscoveryRepository newsDiscoveryRepository,
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    IServiceProvider serviceProvider,
    ILogger<ActionModel> logger) : AdminNewsSuggestionsPageModel
{
    [BindProperty]
    public string? ReviewNotes { get; set; }

    [BindProperty]
    public int? DuplicateCandidateId { get; set; }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyStatusAsync(
            id,
            NewsSuggestionStatus.Rejected,
            successMessage: "Suggestion rejected.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostUnderReviewAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyStatusAsync(
            id,
            NewsSuggestionStatus.UnderReview,
            successMessage: "Marked under review.",
            cancellationToken);
    }

    public async Task<IActionResult> OnPostMarkDuplicateAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await newsSuggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return NotFound();
        }

        if (!NewsSuggestionStatus.IsActive(suggestion.Status))
        {
            return RedirectWithMessage(id, "Only pending suggestions can be marked as duplicates.", "error");
        }

        if (DuplicateCandidateId is not int candidateId || candidateId <= 0)
        {
            return RedirectWithMessage(id, "A valid discovery candidate ID is required.", "error");
        }

        var candidate = await newsDiscoveryRepository.GetCandidateByIdAsync(candidateId, cancellationToken);
        if (candidate is null)
        {
            return RedirectWithMessage(id, "Discovery candidate was not found.", "error");
        }

        var updated = await newsSuggestionRepository.MarkDuplicateAsync(
            id,
            candidateId,
            EditorEmail,
            ReviewNotes,
            cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        TempData["NewsSuggestionMessage"] = "Suggestion marked as duplicate.";
        TempData["NewsSuggestionMessageKind"] = "success";
        return Redirect($"/admin/news-suggestions/{id}");
    }

    public async Task<IActionResult> OnPostPromoteAsync(Guid id, CancellationToken cancellationToken)
    {
        var suggestion = await newsSuggestionRepository.GetByIdAsync(id, cancellationToken);
        if (suggestion is null)
        {
            return NotFound();
        }

        if (!NewsSuggestionStatus.IsActive(suggestion.Status))
        {
            return RedirectWithMessage(id, "Only pending suggestions can be promoted.", "error");
        }

        var adminDraft = NewsSuggestionPromoteDraft.Build(suggestion);
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(adminDraft.Title, adminDraft.Slug),
            cancellationToken: cancellationToken);
        var validationErrors = NewsValidation.ValidateDraft(adminDraft, slugInUse);
        if (validationErrors.Count > 0)
        {
            return RedirectWithMessage(id, string.Join(" ", validationErrors), "error");
        }

        await using var transaction = serviceProvider.GetService<QueenZoneDbContext>() is { } dbContext
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        int newsId;
        var promotionStage = "creating the admin draft";
        try
        {
            newsId = await adminNewsRepository.CreateDraftAsync(adminDraft, EditorEmail, cancellationToken);

            promotionStage = "updating the suggestion";
            var promoted = await newsSuggestionRepository.PromoteAsync(
                id,
                newsId,
                EditorEmail,
                ReviewNotes,
                cancellationToken);
            if (promoted is null)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return RedirectWithMessage(id, "Promotion failed while updating the suggestion.", "error");
            }

            promotionStage = "recording the promotion audit";
            await auditRepository.AppendAsync(
                newsId,
                "promote-from-suggestion",
                EditorEmail,
                $"Promoted from member suggestion {id}. URL: {suggestion.Url}",
                cancellationToken);

            if (transaction is not null)
            {
                promotionStage = "committing the promotion";
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            logger.LogError(
                ex,
                "Failed while {PromotionStage} for news suggestion {SuggestionId}",
                promotionStage,
                id);
            return RedirectWithMessage(
                id,
                $"Promotion failed while {promotionStage}. Check the app logs for details.",
                "error");
        }

        return Redirect($"/admin/news/{newsId}/edit");
    }

    private async Task<IActionResult> ApplyStatusAsync(
        Guid id,
        string status,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await newsSuggestionRepository.UpdateStatusAsync(
                id,
                status,
                EditorEmail,
                ReviewNotes,
                cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            TempData["NewsSuggestionMessage"] = successMessage;
            TempData["NewsSuggestionMessageKind"] = "success";
        }
        catch (ArgumentException ex)
        {
            TempData["NewsSuggestionMessage"] = ex.Message;
            TempData["NewsSuggestionMessageKind"] = "error";
        }

        return Redirect($"/admin/news-suggestions/{id}");
    }

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["NewsSuggestionMessage"] = message;
        TempData["NewsSuggestionMessageKind"] = kind;
        return Redirect($"/admin/news-suggestions/{id}");
    }
}
