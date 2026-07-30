using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Biography;

public sealed class EditPostModel(
    IBiographyRepository biographyRepository,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore,
    UgcHtml ugcHtml) : AdminBiographyPageModel
{
    public ChapterFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        int id,
        [FromForm] AdminBiographyForm form,
        CancellationToken cancellationToken)
    {
        var existing = await biographyRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var rawDraft = form.ToDraft();
        var draft = rawDraft with { Body = ugcHtml.Sanitize(rawDraft.Body) };
        var errors = BiographyValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Edit biography chapter";
            Form = EditModel.BuildForm(existing, draft, errors);
            return Page();
        }

        await biographyRepository.UpdateAsync(id, draft, cancellationToken);
        await IndexModel.InvalidatePublicBiographyCachesAsync(
            coreSitemapService,
            outputCacheStore,
            cancellationToken);

        TempData[MessageKey] = $"Saved \"{draft.Title}\".";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/biography/{id}/edit");
    }
}
