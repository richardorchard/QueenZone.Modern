using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class ForumModel(PublicQueryCacheService publicQueryCache) : PageModel
{
    public ForumIndexStats Stats { get; private set; } = new(0, 0, 0);

    public IReadOnlyList<ForumCategorySummary> Categories { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Forum", "/forum")];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var categories = await publicQueryCache.GetForumCategoriesAsync(cancellationToken);
        var threadCount = await publicQueryCache.GetForumThreadCountAsync(cancellationToken);
        Categories = PublicContentMapper.ToForumCategorySummaries(categories);
        Stats = PublicContentMapper.ToForumIndexStats(categories, threadCount);
    }
}
