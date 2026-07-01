using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsDiscovery;

public sealed class IndexModel(INewsDiscoveryRepository discoveryRepository) : AdminNewsDiscoveryPageModel
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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "News discovery review";
        Sources = await discoveryRepository.GetSourcesAsync(cancellationToken: cancellationToken);
        Candidates = await discoveryRepository.ListCandidatesForReviewAsync(
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
    }
}
