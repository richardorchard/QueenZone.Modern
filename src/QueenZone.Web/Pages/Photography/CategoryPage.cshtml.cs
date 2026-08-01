using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class CategoryPageModel(PublicQueryCacheService publicQueryCache) : PhotoCategoryPageModel(publicQueryCache)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            var filter = PhotoListFilter.Parse(Size);
            return RedirectPermanent(PhotoRoutes.GetCategoryPath(slug, filter));
        }

        return await LoadCategoryPageAsync(slug, PageNumber, cancellationToken);
    }
}
