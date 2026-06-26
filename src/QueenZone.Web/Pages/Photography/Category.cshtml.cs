using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class CategoryModel(IPhotoRepository photoRepository) : PhotoCategoryPageModel(photoRepository)
{
    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken) =>
        await LoadCategoryPageAsync(slug, 1, cancellationToken);
}
