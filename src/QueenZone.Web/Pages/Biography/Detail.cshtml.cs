using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Biography;

public sealed class DetailModel(IBiographyRepository biographyRepository) : PageModel
{
    public BiographyChapterDetail? Chapter { get; private set; }

    public BiographyChapterNavViewModel Navigation { get; private set; } = new(null, null);

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var chapter = await biographyRepository.GetByIdAsync(id, cancellationToken);
        if (chapter is null)
        {
            return NotFound();
        }

        var chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        var readingOrder = BiographyChapterOrdering.ByDisplaySequenceAscending(chapters);
        var chapterIndex = readingOrder.ToList().FindIndex(item => item.Id == id);
        var detail = PublicContentMapper.ToBiographyChapterDetail(chapter, chapterIndex);

        var canonicalSlug = NewsSlug.Slugify(chapter.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(detail.DetailPath);
        }

        Chapter = detail;
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Biography", "/biography"),
            new BreadcrumbItem(detail.Title, detail.DetailPath)
        ];
        var navigation = await biographyRepository.GetAdjacentChaptersAsync(id, cancellationToken);
        Navigation = PublicContentMapper.ToBiographyChapterNav(navigation);
        ViewData["Title"] = $"{detail.Title} | QueenZone biography";
        ViewData["CanonicalPath"] = detail.DetailPath;
        ViewData["Description"] = detail.Summary;

        return Page();
    }
}
