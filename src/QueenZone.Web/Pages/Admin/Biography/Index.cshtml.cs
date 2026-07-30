using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Biography;

public sealed class IndexModel(
    IBiographyRepository biographyRepository,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore,
    UgcHtml ugcHtml) : AdminBiographyPageModel
{
    public IReadOnlyList<BiographyChapterItem> Chapters { get; private set; } = [];

    public ChapterFormViewModel? CreateForm { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Chapters = BiographyChapterOrdering.ByDisplaySequenceAscending(
            await biographyRepository.GetChaptersAsync(cancellationToken));
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Biography chapters";
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm] AdminBiographyForm form,
        CancellationToken cancellationToken)
    {
        var rawDraft = form.ToDraft();
        var draft = rawDraft with { Body = ugcHtml.Sanitize(rawDraft.Body) };
        var errors = BiographyValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Create biography chapter";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        var id = await biographyRepository.CreateAsync(draft, cancellationToken);
        await InvalidatePublicBiographyCachesAsync(coreSitemapService, outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Created chapter \"{draft.Title}\".";
        TempData[MessageKindKey] = "success";
        return Redirect($"/admin/biography/{id}/edit");
    }

    internal static async Task InvalidatePublicBiographyCachesAsync(
        CoreSitemapService coreSitemapService,
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken)
    {
        await coreSitemapService.InvalidateAsync(cancellationToken);
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }
}
