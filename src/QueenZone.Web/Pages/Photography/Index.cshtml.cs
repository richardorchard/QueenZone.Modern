using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class IndexModel(PublicQueryCacheService publicQueryCache) : PageModel
{
    public IReadOnlyList<PhotoCategory> Categories { get; private set; } = [];

    public IReadOnlyDictionary<int, string> CoverImageUrls { get; private set; } = new Dictionary<int, string>();

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } =
        [BreadcrumbItem.Home, new BreadcrumbItem("Photography", PhotoRoutes.GetCategoriesPath())];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await publicQueryCache.GetPhotoCategoriesAsync(cancellationToken);

        CoverImageUrls = Categories
            .Where(category => !string.IsNullOrWhiteSpace(category.CoverThumbnailUrl))
            .ToDictionary(category => category.CatId, category => category.CoverThumbnailUrl!);
    }
}
