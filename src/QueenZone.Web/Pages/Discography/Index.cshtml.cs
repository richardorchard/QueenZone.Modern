using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Discography;

public sealed class IndexModel(IDiscographyRepository discographyRepository) : PageModel
{
    public IReadOnlyList<AlbumSummary> Albums { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Discography", DiscographyRoutes.GetIndexPath())];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Albums = await discographyRepository.GetAlbumsAsync(cancellationToken);
    }
}
