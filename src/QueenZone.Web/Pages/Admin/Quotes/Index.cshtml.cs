using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Quotes;

public sealed class IndexModel(
    IQuoteRepository quoteRepository,
    IOutputCacheStore outputCacheStore) : AdminQuotePageModel
{
    public IReadOnlyList<QuoteItem> Quotes { get; private set; } = [];

    public QuoteFormViewModel? CreateForm { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Quotes = await quoteRepository.GetAllAsync(cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Quotes";
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm] AdminQuoteForm form,
        CancellationToken cancellationToken)
    {
        var draft = form.ToDraft();
        var errors = QuoteValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Add quote";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        var id = await quoteRepository.CreateAsync(draft, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = $"Added quote from {draft.WhoSaid}.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/quotes");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await quoteRepository.DeleteAsync(id, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = "Deleted quote.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/quotes");
    }

    public async Task<IActionResult> OnPostTogglePublishAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        await quoteRepository.SetPublishedAsync(id, !isPublished, cancellationToken);
        await InvalidatePublicHomeCacheAsync(outputCacheStore, cancellationToken);
        TempData[MessageKey] = !isPublished ? "Quote published." : "Quote unpublished.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/quotes");
    }

    internal static async Task InvalidatePublicHomeCacheAsync(
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken)
    {
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }
}
