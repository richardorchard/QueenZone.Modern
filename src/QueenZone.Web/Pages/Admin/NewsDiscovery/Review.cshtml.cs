using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class ReviewModel(INewsDiscoveryRepository discoveryRepository) : AdminNewsDiscoveryPageModel
{
    public NewsCandidate? Candidate { get; private set; }

    public IReadOnlyList<NewsCandidateEvidence> Evidence { get; private set; } = [];

    public IReadOnlyList<NewsAiRun> AiRuns { get; private set; } = [];

    public NewsAgentDraft? Draft { get; private set; }

    public NewsCandidate? DuplicateCandidate { get; private set; }

    public NewsTriageDisplaySummary? LatestTriageSummary { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(id, cancellationToken);
        if (!loaded)
        {
            return NotFound();
        }

        StatusMessage = TempData["DiscoveryMessage"] as string;
        StatusMessageKind = TempData["DiscoveryMessageKind"] as string;

        ViewData["Title"] = $"Review candidate #{id}";
        return Page();
    }

    private async Task<bool> LoadAsync(int id, CancellationToken cancellationToken)
    {
        Candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (Candidate is null)
        {
            return false;
        }

        Evidence = await discoveryRepository.GetCandidateEvidenceAsync(id, cancellationToken);
        AiRuns = await discoveryRepository.GetAiRunsForCandidateAsync(id, cancellationToken);
        Draft = await discoveryRepository.GetDraftByCandidateIdAsync(id, cancellationToken);

        if (Candidate.DuplicateOfCandidateId is int duplicateId)
        {
            DuplicateCandidate = await discoveryRepository.GetCandidateByIdAsync(duplicateId, cancellationToken);
        }

        var latestTriage = AiRuns
            .Where(run => run.Kind == NewsAiRunKind.Triage && !string.IsNullOrWhiteSpace(run.StructuredResultJson))
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefault();

        if (latestTriage?.StructuredResultJson is not null
            && NewsAiStructuredDisplay.TryReadTriageSummary(latestTriage.StructuredResultJson, out var summary))
        {
            LatestTriageSummary = summary;
        }

        return true;
    }
}
