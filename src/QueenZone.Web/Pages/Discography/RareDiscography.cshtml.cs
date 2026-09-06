using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Discography;

public sealed class RareDiscographyModel(PublicQueryCacheService publicQueryCache) : PageModel
{
    public IReadOnlyList<ForumRecentThreadSummary> Threads { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } =
    [
        BreadcrumbItem.Home,
        new BreadcrumbItem("Discography", DiscographyRoutes.GetIndexPath()),
        new BreadcrumbItem("Rare Discography", DiscographyRoutes.GetRareDiscographyPath())
    ];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var threads = await publicQueryCache.GetForumLegacyDiscographyThreadsAsync(cancellationToken);
        Threads = PublicContentMapper.ToForumRecentThreadSummaries(threads);
        ViewData["Title"] = "Rare Discography | QueenZone";
        ViewData["Description"] = "John S Stuart's rare Queen discography posts from the QueenZone.com notice board.";
        ViewData["CanonicalPath"] = DiscographyRoutes.GetRareDiscographyPath();
    }
}
