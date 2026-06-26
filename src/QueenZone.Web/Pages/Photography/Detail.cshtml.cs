using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class DetailModel(IPhotoRepository photoRepository) : PageModel
{
    public PhotoCategory Category { get; private set; } = null!;

    public PhotoItem Photo { get; private set; } = null!;

    public int Index { get; private set; }

    public int Count { get; private set; }

    public string? PreviousHref { get; private set; }

    public string? NextHref { get; private set; }

    public string BackToGridHref { get; private set; } = string.Empty;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, int picId, CancellationToken cancellationToken)
    {
        var category = await photoRepository.GetCategoryBySlugAsync(slug, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        var items = await photoRepository.GetCategoryAllAsync(category.CatId, cancellationToken);
        var index = items.ToList().FindIndex(item => item.PicId == picId);
        if (index < 0)
        {
            return NotFound();
        }

        Category = category;
        Photo = items[index];
        Index = index;
        Count = items.Count;

        PreviousHref = index > 0 ? PhotoRoutes.GetDetailPath(category.Slug, items[index - 1].PicId) : null;
        NextHref = index < items.Count - 1 ? PhotoRoutes.GetDetailPath(category.Slug, items[index + 1].PicId) : null;

        var page = (index / PhotoRoutes.CategoryPageSize) + 1;
        BackToGridHref = PhotoRoutes.GetCategoryPagePath(category.Slug, page);

        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Photography", PhotoRoutes.GetCategoriesPath()),
            new BreadcrumbItem(category.Name, PhotoRoutes.GetCategoryPath(category.Slug)),
            new BreadcrumbItem(Photo.Title, PhotoRoutes.GetDetailPath(category.Slug, Photo.PicId)),
        ];

        ViewData["Title"] = $"{Photo.Title} | {category.Name} | Photography | QueenZone";

        return Page();
    }
}
