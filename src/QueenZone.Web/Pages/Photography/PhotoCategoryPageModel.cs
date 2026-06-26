using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public abstract class PhotoCategoryPageModel(IPhotoRepository photoRepository) : PageModel
{
    public PhotoCategory Category { get; private set; } = null!;

    public IReadOnlyList<PhotoItem> Items { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }

    public int RangeStart { get; private set; }

    public int RangeEnd { get; private set; }

    protected async Task<IActionResult> LoadCategoryPageAsync(string slug, int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var category = await photoRepository.GetCategoryBySlugAsync(slug, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var result = await photoRepository.GetCategoryPageAsync(category.CatId, page, PhotoRoutes.CategoryPageSize, cancellationToken);
        var totalPages = PhotoRoutes.GetCategoryTotalPages(result.TotalCount);

        if (totalPages == 0 ? page > 1 : page > totalPages)
        {
            return NotFound();
        }

        Category = category;
        Items = result.Items;
        CurrentPage = page;
        TotalPages = totalPages;
        TotalCount = result.TotalCount;
        RangeStart = result.TotalCount == 0 ? 0 : ((page - 1) * PhotoRoutes.CategoryPageSize) + 1;
        RangeEnd = result.TotalCount == 0 ? 0 : RangeStart + result.Items.Count - 1;

        ViewData["Title"] = page <= 1 ? $"{category.Name} | Photography | QueenZone" : $"{category.Name} | Photography – Page {page} | QueenZone";
        ViewData["CanonicalPath"] = PhotoRoutes.GetCategoryPagePath(category.Slug, page);

        return Page();
    }
}
