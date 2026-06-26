using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class CategoryPageModel(IPhotoRepository photoRepository) : PhotoCategoryPageModel(photoRepository)
{
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        if (PageNumber == 1)
        {
            return RedirectPermanent(PhotoRoutes.GetCategoryPath(slug));
        }

        return await LoadCategoryPageAsync(slug, PageNumber, cancellationToken);
    }
}
