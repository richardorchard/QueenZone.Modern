using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class IndexModel(IPhotoRepository photoRepository) : PageModel
{
    public IReadOnlyList<PhotoCategorySummary> Categories { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Photography", PhotoRoutes.GetCategoriesPath())];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var categories = await photoRepository.GetCategoriesAsync(cancellationToken);

        var covers = new Dictionary<int, string>();
        foreach (var category in categories)
        {
            var page = await photoRepository.GetCategoryPageAsync(category.CatId, 1, 1, cancellationToken);
            var cover = page.Items.FirstOrDefault();
            if (cover is not null)
            {
                covers[category.CatId] = cover.ThumbnailUrl;
            }
        }

        Categories = PublicContentMapper.ToPhotoCategorySummaries(categories, covers);
    }
}
