using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class ActionModel(
    INewsDiscoveryRepository discoveryRepository,
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    NewsDraftGenerationService draftGenerationService,
    IServiceProvider serviceProvider,
    ILogger<ActionModel> logger) : AdminNewsDiscoveryPageModel
{
    public async Task<IActionResult> OnPostRejectAsync(int id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        if (!NewsCandidateWorkflow.CanTransition(candidate.Status, NewsCandidateStatus.Rejected))
        {
            return RedirectToReview(
                id,
                candidate.Status == NewsCandidateStatus.Rejected
                    ? "This candidate has already been rejected."
                    : $"Cannot transition candidate status from {candidate.Status} to {NewsCandidateStatus.Rejected}.");
        }

        var updated = await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.Rejected,
                ReviewNotes: $"Marked not relevant by {EditorEmail}."),
            cancellationToken);
        if (!updated)
        {
            return RedirectToReview(id, "The candidate could not be marked as rejected.");
        }

        return Redirect("/admin/news-discovery");
    }

    public async Task<IActionResult> OnPostIgnoreDuplicateAsync(int id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        if (!NewsCandidateWorkflow.CanTransition(candidate.Status, NewsCandidateStatus.IgnoredDuplicate))
        {
            return RedirectToReview(
                id,
                candidate.Status == NewsCandidateStatus.IgnoredDuplicate
                    ? "This candidate has already been ignored as a duplicate."
                    : $"Cannot transition candidate status from {candidate.Status} to {NewsCandidateStatus.IgnoredDuplicate}.");
        }

        var duplicateOf = candidate.DuplicateOfCandidateId
            ?? (await discoveryRepository.FindEarlierDuplicateCandidateAsync(
                candidate.Id,
                candidate.SourceTitle,
                candidate.ContentHash,
                cancellationToken))?.Id;

        var updated = await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.IgnoredDuplicate,
                ReviewNotes: $"Ignored as duplicate by {EditorEmail}.",
                DuplicateOfCandidateId: duplicateOf),
            cancellationToken);
        if (!updated)
        {
            return RedirectToReview(id, "The candidate could not be marked as a duplicate.");
        }

        return Redirect("/admin/news-discovery");
    }

    public async Task<IActionResult> OnPostPromoteAsync(int id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        var agentDraft = await discoveryRepository.GetDraftByCandidateIdAsync(id, cancellationToken);
        if (agentDraft is null)
        {
            return RedirectToReview(id, "Generate or save a draft before promoting this candidate.");
        }

        if (candidate.Status == NewsCandidateStatus.NeedsReview)
        {
            if (!NewsCandidateWorkflow.TryTransition(candidate.Status, NewsCandidateStatus.Drafted, out var draftedError))
            {
                return RedirectToReview(id, draftedError);
            }

            var drafted = await discoveryRepository.TryUpdateCandidateStatusAsync(
                id,
                new NewsCandidateStatusUpdate(
                    NewsCandidateStatus.Drafted,
                    ReviewNotes: $"Draft acknowledged by {EditorEmail} before promotion."),
                cancellationToken);
            if (!drafted)
            {
                return RedirectToReview(id, "The candidate could not be marked as drafted before promotion.");
            }

            candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        }

        if (candidate is null || candidate.Status != NewsCandidateStatus.Drafted)
        {
            return RedirectToReview(
                id,
                candidate is null
                    ? "The candidate is no longer available."
                    : $"Only drafted candidates can be promoted. Current status: {candidate.Status}.");
        }

        var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(adminDraft.Title, adminDraft.Slug),
            cancellationToken: cancellationToken);
        var validationErrors = NewsValidation.ValidateDraft(adminDraft, slugInUse);
        if (validationErrors.Count > 0)
        {
            return RedirectToReview(id, string.Join(" ", validationErrors));
        }

        await using var transaction = serviceProvider.GetService<QueenZoneDbContext>() is { } dbContext
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        int newsId;
        var promotionStage = "creating the admin draft";
        try
        {
            newsId = await adminNewsRepository.CreateDraftAsync(adminDraft, EditorEmail, cancellationToken);

            if (!NewsCandidateWorkflow.TryTransition(candidate.Status, NewsCandidateStatus.PromotedToArticle, out var promoteError))
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return RedirectToReview(id, promoteError);
            }

            promotionStage = "updating the discovery candidate";
            var promoted = await discoveryRepository.TryUpdateCandidateStatusAsync(
                id,
                new NewsCandidateStatusUpdate(
                    NewsCandidateStatus.PromotedToArticle,
                    ReviewNotes: $"Promoted to admin news draft #{newsId} by {EditorEmail} at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.",
                    PromotedNewsId: newsId),
                cancellationToken);
            if (!promoted)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return RedirectToReview(id, "Promotion failed while updating the discovery candidate.");
            }

            promotionStage = "loading the discovery audit provenance";
            var aiRuns = await discoveryRepository.GetAiRunsForCandidateAsync(id, cancellationToken);
            var provenance = NewsDiscoveryProvenanceBuilder.Build(candidate, agentDraft, aiRuns);

            promotionStage = "recording the promotion audit";
            await auditRepository.AppendAsync(
                newsId,
                "promote-from-discovery",
                EditorEmail,
                NewsDiscoveryPromoteAudit.Format(provenance),
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
                "Failed while {PromotionStage} for discovery candidate {CandidateId}",
                promotionStage,
                id);
            return RedirectToReview(
                id,
                $"Promotion failed while {promotionStage}. Check the app logs for the exact validation or database error.");
        }

        return Redirect($"/admin/news/{newsId}/edit");
    }

    public async Task<IActionResult> OnPostRegenerateDraftAsync(int id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        if (!draftGenerationService.IsAiEnabled)
        {
            TempData["DiscoveryMessage"] = "Draft regeneration requires OpenRouter configuration on the web app.";
            TempData["DiscoveryMessageKind"] = "error";
            return Redirect($"/admin/news-discovery/{id}");
        }

        if (candidate.Status is not NewsCandidateStatus.NeedsReview and not NewsCandidateStatus.Drafted)
        {
            TempData["DiscoveryMessage"] = "Only needs-review or drafted candidates can regenerate a draft.";
            TempData["DiscoveryMessageKind"] = "error";
            return Redirect($"/admin/news-discovery/{id}");
        }

        try
        {
            var result = await draftGenerationService.GenerateDraftAsync(
                candidate,
                new NewsDraftRunOptions(ForceRegenerate: true),
                cancellationToken);

            if (result.Succeeded && result.DraftId is not null)
            {
                TempData["DiscoveryMessage"] = "Draft regenerated successfully.";
                TempData["DiscoveryMessageKind"] = "success";
            }
            else
            {
                TempData["DiscoveryMessage"] = "Draft regeneration did not produce a new draft.";
                TempData["DiscoveryMessageKind"] = "error";
            }
        }
        catch (Exception ex)
        {
            TempData["DiscoveryMessage"] = $"Draft regeneration failed: {ex.Message}";
            TempData["DiscoveryMessageKind"] = "error";
        }

        return Redirect($"/admin/news-discovery/{id}");
    }

    private IActionResult RedirectToReview(int id, string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            TempData["DiscoveryMessage"] = message;
            TempData["DiscoveryMessageKind"] = "error";
        }

        return Redirect($"/admin/news-discovery/{id}");
    }
}
