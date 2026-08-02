using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class IndexModel(
    INewsDiscoveryRepository discoveryRepository,
    INewsAgentRunRequestRepository runRequestRepository) : AdminNewsDiscoveryPageModel
{
    [BindProperty(SupportsGet = true)]
    public NewsCandidateStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SourceId { get; set; }

    [BindProperty(SupportsGet = true)]
    public NewsDiscoveryTrustTier? TrustTier { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinConfidence { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Entity { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DiscoveredFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DiscoveredTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? HasDraft { get; set; }

    public IReadOnlyList<NewsCandidateReviewListItem> Candidates { get; private set; } = [];

    public IReadOnlyList<NewsDiscoverySource> Sources { get; private set; } = [];

    public IReadOnlyList<NewsAgentRunRequest> RecentRuns { get; private set; } = [];

    public NewsAgentRunnerHeartbeat? RunnerHeartbeat { get; private set; }

    public bool RunnerRecentlySeen =>
        RunnerHeartbeat is not null && RunnerHeartbeat.LastSeenAtUtc >= DateTime.UtcNow.AddMinutes(-5);

    public string? StatusMessage => TempData["DiscoveryMessage"] as string;

    public string? StatusMessageKind => TempData["DiscoveryMessageKind"] as string;

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostQueueRunAsync(CancellationToken cancellationToken)
    {
        var result = await runRequestRepository.QueueAsync(EditorEmail, cancellationToken);
        TempData["DiscoveryMessage"] = result.WasCreated
            ? "News gathering queued. The local Windows runner will fetch and triage it when next online."
            : $"A news gathering run is already {result.Request.Status.ToString().ToLowerInvariant()}.";
        TempData["DiscoveryMessageKind"] = result.WasCreated ? "success" : "info";
        return Redirect(BuildReturnUrl());
    }

    public async Task<IActionResult> OnPostIgnoreAsync(int id, CancellationToken cancellationToken)
    {
        var candidate = await discoveryRepository.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate is null)
        {
            return NotFound();
        }

        if (!NewsCandidateWorkflow.CanReject(candidate.Status))
        {
            TempData["DiscoveryMessage"] = NewsCandidateWorkflow.GetRejectError(candidate.Status);
            TempData["DiscoveryMessageKind"] = "error";
            return Redirect(BuildReturnUrl());
        }

        var ignored = await discoveryRepository.TryUpdateCandidateStatusAsync(
            id,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.Rejected,
                ReviewNotes: $"Ignored from discovery listing by {EditorEmail}."),
            cancellationToken);

        TempData["DiscoveryMessage"] = ignored
            ? $"Ignored candidate #{id}."
            : $"Candidate #{id} could not be ignored.";
        TempData["DiscoveryMessageKind"] = ignored ? "success" : "error";
        return Redirect(BuildReturnUrl());
    }

    public async Task<IActionResult> OnPostIgnoreListedAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        var ignoreable = Candidates
            .Where(candidate => NewsCandidateWorkflow.CanReject(candidate.Status))
            .ToList();

        var ignored = 0;
        foreach (var candidate in ignoreable)
        {
            var updated = await discoveryRepository.TryUpdateCandidateStatusAsync(
                candidate.Id,
                new NewsCandidateStatusUpdate(
                    NewsCandidateStatus.Rejected,
                    ReviewNotes: $"Bulk ignored from discovery listing by {EditorEmail}."),
                cancellationToken);

            if (updated)
            {
                ignored++;
            }
        }

        TempData["DiscoveryMessage"] = ignored == 1
            ? "Ignored 1 listed candidate."
            : $"Ignored {ignored} listed candidates.";
        TempData["DiscoveryMessageKind"] = ignored > 0 ? "success" : "info";
        return Redirect(BuildReturnUrl());
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "News discovery review";
        RecentRuns = await runRequestRepository.ListRecentAsync(5, cancellationToken);
        RunnerHeartbeat = await runRequestRepository.GetLatestHeartbeatAsync(cancellationToken);
        Sources = await discoveryRepository.GetSourcesAsync(cancellationToken: cancellationToken);
        var candidates = await discoveryRepository.ListCandidatesForReviewAsync(
            new NewsCandidateListQuery(
                Status: Status,
                SourceId: SourceId,
                TrustTier: TrustTier,
                MinConfidence: MinConfidence,
                Entity: string.IsNullOrWhiteSpace(Entity) ? null : Entity.Trim(),
                DiscoveredFromUtc: DiscoveredFrom?.ToUniversalTime(),
                DiscoveredToUtc: DiscoveredTo?.AddDays(1).ToUniversalTime(),
                HasDraft: HasDraft),
            cancellationToken);

        Candidates = Status is null
            ? candidates
                .Where(candidate => candidate.Status is not NewsCandidateStatus.Rejected
                    and not NewsCandidateStatus.IgnoredDuplicate
                    and not NewsCandidateStatus.PromotedToArticle)
                .ToList()
            : candidates;
    }

    private string BuildReturnUrl()
    {
        var query = new Dictionary<string, string?>();
        if (Status is not null)
        {
            query["status"] = Status.Value.ToString();
        }

        if (SourceId is not null)
        {
            query["sourceId"] = SourceId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (TrustTier is not null)
        {
            query["trustTier"] = TrustTier.Value.ToString();
        }

        if (MinConfidence is not null)
        {
            query["minConfidence"] = MinConfidence.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(Entity))
        {
            query["entity"] = Entity.Trim();
        }

        if (DiscoveredFrom is not null)
        {
            query["discoveredFrom"] = DiscoveredFrom.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (DiscoveredTo is not null)
        {
            query["discoveredTo"] = DiscoveredTo.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (HasDraft is not null)
        {
            query["hasDraft"] = HasDraft.Value ? "true" : "false";
        }

        return query.Count == 0
            ? "/admin/news-discovery"
            : QueryHelpers.AddQueryString("/admin/news-discovery", query);
    }
}
