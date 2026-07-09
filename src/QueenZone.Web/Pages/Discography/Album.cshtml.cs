using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Discography;

public sealed class AlbumModel(IDiscographyRepository discographyRepository) : PageModel
{
    public AlbumDetailViewModel Album { get; private set; } = null!;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var album = await discographyRepository.GetAlbumByIdAsync(id, cancellationToken);
        if (album is null)
        {
            return NotFound();
        }

        var detail = PublicContentMapper.ToAlbumDetailViewModel(album);
        if (!string.Equals(album.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(detail.DetailPath);
        }

        Album = detail;
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Discography", DiscographyRoutes.GetIndexPath()),
            new BreadcrumbItem(detail.Name, detail.DetailPath)
        ];
        ViewData["Title"] = $"{detail.Name} | Discography | QueenZone";
        ViewData["CanonicalPath"] = detail.DetailPath;
        if (detail.GeneralNotes is not null)
        {
            ViewData["Description"] = NewsArticleContent.ToPlainText(detail.GeneralNotes);
        }

        return Page();
    }
}
