using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class IndexModel(
    PublicQueryCacheService publicQueryCache,
    TimeProvider timeProvider) : PageModel
{
    public IReadOnlyList<NewsItem> Latest { get; private set; } = [];

    public IReadOnlyList<QueenHistoryEvent> OnThisDay { get; private set; } = [];

    public bool IsOnThisDayFallback { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "QueenZone";
        Latest = await publicQueryCache.GetLatestNewsAsync(5, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        OnThisDay = await publicQueryCache.GetOnThisDayAsync(today, 3, cancellationToken);

        if (OnThisDay.Count == 0)
        {
            OnThisDay = await publicQueryCache.GetAroundThisDayAsync(today, 7, 3, cancellationToken);
            IsOnThisDayFallback = OnThisDay.Count > 0;
        }
    }
}
