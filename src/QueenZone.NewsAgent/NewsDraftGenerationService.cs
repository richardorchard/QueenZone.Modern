using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsDraftGenerationService(
    INewsDiscoveryRepository repository,
    NewsAiRunExecutor aiRunExecutor,
    IOptions<NewsDraftGenerationOptions> draftOptions,
    ILogger<NewsDraftGenerationService> logger)
{
    public bool IsAiEnabled => aiRunExecutor.IsAiEnabled;

    public async Task<NewsDraftRunResult> RunDraftGenerationAsync(
        NewsDraftRunOptions options,
        CancellationToken cancellationToken = default)
    {
        draftOptions.Value.Validate();

        if (!aiRunExecutor.IsAiEnabled)
        {
            logger.LogWarning("OpenRouter is disabled; draft generation skipped.");
            return new NewsDraftRunResult(0, 0, 0, 0, []);
        }

        var limit = options.PerRunCandidateLimit ?? draftOptions.Value.PerRunCandidateLimit;
        var candidates = await repository.GetCandidatesAsync(
            NewsCandidateStatus.NeedsReview,
            cancellationToken: cancellationToken);
        var toProcess = candidates
            .Where(candidate => MeetsConfidenceThreshold(candidate))
            .Take(limit)
            .ToList();

        if (toProcess.Count == 0)
        {
            logger.LogInformation("No needs-review candidates are ready for draft generation.");
            return new NewsDraftRunResult(0, 0, 0, 0, []);
        }

        aiRunExecutor.BeginRun();

        var created = 0;
        var skipped = 0;
        var failures = 0;
        var errors = new List<string>();

        foreach (var candidate in toProcess)
        {
            try
            {
                var result = await GenerateDraftAsync(candidate, options, cancellationToken);
                if (result.Succeeded && result.DraftId is not null)
                {
                    created++;
                }
                else
                {
                    skipped++;
                }
            }
            catch (Exception ex)
            {
                failures++;
                errors.Add($"Candidate {candidate.Id}: {ex.Message}");
                logger.LogError(ex, "Draft generation failed for candidate {CandidateId}.", candidate.Id);
            }
        }

        return new NewsDraftRunResult(toProcess.Count, created, skipped, failures, errors);
    }

    public async Task<NewsDraftCandidateResult> GenerateDraftAsync(
        NewsCandidate candidate,
        NewsDraftRunOptions options,
        CancellationToken cancellationToken = default)
    {
        var draftStatusError = NewsCandidateWorkflow.GetDraftGenerationError(candidate.Status);
        if (!string.IsNullOrEmpty(draftStatusError))
        {
            throw new InvalidOperationException($"Candidate {candidate.Id}: {draftStatusError}");
        }

        if (candidate.Status == NewsCandidateStatus.Drafted && !options.ForceRegenerate)
        {
            throw new InvalidOperationException($"Candidate {candidate.Id} already has a draft. Use force regenerate.");
        }

        if (!MeetsConfidenceThreshold(candidate))
        {
            throw new InvalidOperationException($"Candidate {candidate.Id} is below the configured confidence threshold.");
        }

        var existingDraft = await repository.GetDraftByCandidateIdAsync(candidate.Id, cancellationToken);
        if (existingDraft is not null && !options.ForceRegenerate)
        {
            logger.LogInformation("Skipping candidate {CandidateId}; draft already exists.", candidate.Id);
            return new NewsDraftCandidateResult(candidate.Id, existingDraft.Id, false, null);
        }

        var source = await repository.GetSourceByIdAsync(candidate.SourceId, cancellationToken)
            ?? throw new InvalidOperationException($"Discovery source {candidate.SourceId} was not found.");

        var evidence = await repository.GetCandidateEvidenceAsync(candidate.Id, cancellationToken);
        var messages = NewsDraftPrompt.BuildMessages(candidate, source, evidence);
        var execution = await aiRunExecutor.ExecuteAsync(
            candidate.Id,
            NewsAiRunKind.DraftGeneration,
            NewsAiModelRole.Drafting,
            NewsDraftPrompt.Version,
            messages,
            cancellationToken: cancellationToken);

        if (execution.Completion.DryRun || string.IsNullOrWhiteSpace(execution.Completion.Content))
        {
            logger.LogInformation(
                "Skipped draft persistence for candidate {CandidateId} because dry-run returned no structured content.",
                candidate.Id);
            return new NewsDraftCandidateResult(candidate.Id, null, false, null);
        }

        var structuredDraft = NewsDraftResultParser.Parse(execution.Completion.Content);
        var attribution = NewsDraftAttributionBuilder.Build(structuredDraft, candidate, source, evidence);
        var proposedSlug = NewsSlug.Slugify(
            structuredDraft.Slug ?? structuredDraft.Title);

        if (!options.DryRun)
        {
            var draftId = await repository.UpsertDraftAsync(
                candidate.Id,
                new NewsAgentDraftUpsert(
                    structuredDraft.Title,
                    proposedSlug,
                    structuredDraft.Excerpt,
                    structuredDraft.Body,
                    attribution.AttributionText,
                    attribution.SourceNotes,
                    attribution.ConfidenceNotes,
                    structuredDraft.SuggestedPublishAt,
                    execution.AiRunId),
                cancellationToken);

            if (candidate.Status != NewsCandidateStatus.Drafted
                && !NewsCandidateWorkflow.TryValidateStatusChange(
                    candidate.Status,
                    NewsCandidateStatus.Drafted,
                    out var markDraftedError))
            {
                throw new InvalidOperationException(
                    $"Candidate {candidate.Id}: {markDraftedError}");
            }

            if (candidate.Status != NewsCandidateStatus.Drafted)
            {
                var updated = await repository.TryUpdateCandidateStatusAsync(
                    candidate.Id,
                    new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted),
                    cancellationToken);

                if (!updated)
                {
                    throw new InvalidOperationException($"Failed to mark candidate {candidate.Id} as drafted.");
                }
            }

            logger.LogInformation(
                "Generated draft {DraftId} for candidate {CandidateId}.",
                draftId,
                candidate.Id);

            return new NewsDraftCandidateResult(candidate.Id, draftId, true, null);
        }

        logger.LogInformation("Dry-run draft generated for candidate {CandidateId}.", candidate.Id);
        return new NewsDraftCandidateResult(candidate.Id, null, true, null);
    }

    private bool MeetsConfidenceThreshold(NewsCandidate candidate)
    {
        var minimum = draftOptions.Value.MinConfidenceScore(candidate.SourceTrustTier);
        return (candidate.ConfidenceScore ?? 0m) >= minimum;
    }
}
