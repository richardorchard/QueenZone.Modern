using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class ActionModel(
    INewsDiscoveryRepository discoveryRepository,
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    NewsDraftGenerationService draftGenerationService) : AdminNewsDiscoveryPageModel
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
            return Redirect($"/admin/news-discovery/{id}");
        }

        await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.Rejected,
                ReviewNotes: $"Marked not relevant by {EditorEmail}."),
            cancellationToken);

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
            return Redirect($"/admin/news-discovery/{id}");
        }

        var duplicateOf = candidate.DuplicateOfCandidateId
            ?? (await discoveryRepository.FindEarlierDuplicateCandidateAsync(
                candidate.Id,
                candidate.SourceTitle,
                candidate.ContentHash,
                cancellationToken))?.Id;

        await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.IgnoredDuplicate,
                ReviewNotes: $"Ignored as duplicate by {EditorEmail}.",
                DuplicateOfCandidateId: duplicateOf),
            cancellationToken);

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
            return Redirect($"/admin/news-discovery/{id}");
        }

        if (candidate.Status == NewsCandidateStatus.NeedsReview)
        {
            var drafted = await discoveryRepository.TryUpdateCandidateStatusAsync(
                id,
                new NewsCandidateStatusUpdate(
                    NewsCandidateStatus.Drafted,
                    ReviewNotes: $"Draft acknowledged by {EditorEmail} before promotion."),
                cancellationToken);
            if (!drafted)
            {
                return Redirect($"/admin/news-discovery/{id}");
            }

            candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        }

        if (candidate is null || candidate.Status != NewsCandidateStatus.Drafted)
        {
            return Redirect($"/admin/news-discovery/{id}");
        }

        var body = agentDraft.ProposedBody;
        if (!string.IsNullOrWhiteSpace(agentDraft.AttributionText))
        {
            body = $"{body.TrimEnd()}\n\n{agentDraft.AttributionText.Trim()}";
        }

        var newsId = await adminNewsRepository.CreateDraftAsync(
            new AdminNewsDraft(
                agentDraft.ProposedTitle,
                agentDraft.ProposedSlug,
                agentDraft.ProposedExcerpt,
                body,
                agentDraft.SuggestedPublishAt ?? DateTime.UtcNow.Date,
                candidate.SourceUrl),
            EditorEmail,
            cancellationToken);

        var promoted = await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.PromotedToArticle,
                ReviewNotes: $"Promoted to admin news draft #{newsId} by {EditorEmail} at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.",
                PromotedNewsId: newsId),
            cancellationToken);
        if (!promoted)
        {
            return Redirect($"/admin/news-discovery/{id}");
        }

        var aiRuns = await discoveryRepository.GetAiRunsForCandidateAsync(id, cancellationToken);
        var provenance = NewsDiscoveryProvenanceBuilder.Build(candidate, agentDraft, aiRuns);

        await auditRepository.AppendAsync(
            newsId,
            "promote-from-discovery",
            EditorEmail,
            NewsDiscoveryPromoteAudit.Format(provenance),
            cancellationToken);

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
}
