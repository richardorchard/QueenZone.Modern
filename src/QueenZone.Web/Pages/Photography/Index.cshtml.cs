using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Photography;

public sealed class IndexModel(IPhotoRepository photoRepository) : PageModel
{
    public IReadOnlyList<PhotoCategory> Categories { get; private set; } = [];

    public IReadOnlyDictionary<int, string> CoverImageUrls { get; private set; } = new Dictionary<int, string>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await photoRepository.GetCategoriesAsync(cancellationToken);

        var covers = new Dictionary<int, string>();
        foreach (var category in Categories)
        {
            var page = await photoRepository.GetCategoryPageAsync(category.CatId, 1, 1, cancellationToken);
            var cover = page.Items.FirstOrDefault();
            if (cover is not null)
            {
                covers[category.CatId] = cover.ThumbnailUrl;
            }
        }

        CoverImageUrls = covers;
    }
}
