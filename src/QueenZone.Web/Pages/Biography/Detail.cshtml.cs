using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Biography;

public sealed class DetailModel(IBiographyRepository biographyRepository) : PageModel
{
    public BiographyChapterItem? Chapter { get; private set; }

    public BiographyChapterNav Navigation { get; private set; } = new(null, null);

    public int ChapterIndex { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, string slug, CancellationToken cancellationToken)
    {
        var chapter = await biographyRepository.GetByIdAsync(id, cancellationToken);
        if (chapter is null)
        {
            return NotFound();
        }

        var canonicalSlug = NewsSlug.Slugify(chapter.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(BiographyRoutes.GetChapterDetailPath(chapter));
        }

        var chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        ChapterIndex = chapters.ToList().FindIndex(item => item.Id == id);

        Chapter = chapter;
        Navigation = await biographyRepository.GetAdjacentChaptersAsync(id, cancellationToken);
        ViewData["Title"] = $"{chapter.Title} | QueenZone biography";
        ViewData["CanonicalPath"] = BiographyContent.GetDetailCanonicalPath(chapter);
        ViewData["Description"] = chapter.Summary;

        return Page();
    }
}