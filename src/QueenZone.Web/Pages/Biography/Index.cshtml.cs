using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Biography;

public sealed class IndexModel(IBiographyRepository biographyRepository) : PageModel
{
    public IReadOnlyList<BiographyChapterItem> Chapters { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Biography", "/biography")];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        ViewData["Title"] = "QueenZone biography";
        ViewData["CanonicalPath"] = BiographyRoutes.IndexPath;
        ViewData["Description"] = "The story of Queen, told in chapters from the preserved Queenzone.com archive.";
    }
}