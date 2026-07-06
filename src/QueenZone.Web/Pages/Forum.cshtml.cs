using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class ForumModel(PublicQueryCacheService publicQueryCache) : PageModel
{
    public ForumArchiveStats Stats { get; private set; } = new(0, 0, 0);

    public IReadOnlyList<ForumCategoryItem> Categories { get; private set; } = Array.Empty<ForumCategoryItem>();

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Forum", "/forum")];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await publicQueryCache.GetForumCategoriesAsync(cancellationToken);
        var threadCount = await publicQueryCache.GetForumThreadCountAsync(cancellationToken);
        Stats = ForumArchiveStats.FromCategories(Categories, threadCount);
    }
}
