using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class LinksModel(
    ILinksRepository linksRepository) : PageModel
{
    public IReadOnlyList<QueenLinkCategory> Categories { get; private set; } = [];

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; } = [BreadcrumbItem.Home, new BreadcrumbItem("Links", "/links")];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Categories = await linksRepository.GetCategoriesWithLinksAsync(cancellationToken);

        ViewData["Title"] = "QueenZone links";
        ViewData["CanonicalPath"] = "/links";
        ViewData["Description"] = "A checked directory of Queen-related websites from the preserved Queenzone archive.";
    }
}
