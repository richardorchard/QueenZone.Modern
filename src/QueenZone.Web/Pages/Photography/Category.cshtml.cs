using Microsoft.AspNetCore.Mvc;

namespace QueenZone.Web.Pages.Photography;

public sealed class CategoryModel(PublicQueryCacheService publicQueryCache) : PhotoCategoryPageModel(publicQueryCache)
{
    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken) =>
        await LoadCategoryPageAsync(slug, 1, cancellationToken);
}
