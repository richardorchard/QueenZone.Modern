using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Timeline;

public sealed class IndexModel(
    IAdminQueenHistoryRepository historyRepository,
    IOutputCacheStore outputCacheStore,
    PublicQueryCacheService publicQueryCache) : AdminTimelinePageModel
{
    public const int PageSize = 50;

    [BindProperty(SupportsGet = true)]
    public string? Published { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public AdminQueenHistoryPage? Events { get; private set; }

    public TimelineFormViewModel? CreateForm { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Events = await historyRepository.GetPageAsync(BuildFilter(), PageNumber, PageSize, cancellationToken);
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        ViewData["Title"] = "Timeline";
    }

    public async Task<IActionResult> OnPostAsync(
        [FromForm] AdminTimelineForm form,
        CancellationToken cancellationToken)
    {
        var draft = form.ToDraft();
        var errors = QueenHistoryValidation.ValidateDraft(draft);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Add timeline event";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        await historyRepository.CreateAsync(draft, cancellationToken);
        await InvalidatePublicHistoryCacheAsync(outputCacheStore, publicQueryCache, cancellationToken);
        TempData[MessageKey] = $"Added timeline event {draft.Title}.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/timeline");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        await historyRepository.DeleteAsync(id, cancellationToken: cancellationToken);
        await InvalidatePublicHistoryCacheAsync(outputCacheStore, publicQueryCache, cancellationToken);
        TempData[MessageKey] = "Deleted timeline event.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/timeline");
    }

    public async Task<IActionResult> OnPostTogglePublishAsync(
        int id,
        bool isPublished,
        CancellationToken cancellationToken)
    {
        await historyRepository.SetPublishedAsync(id, !isPublished, cancellationToken: cancellationToken);
        await InvalidatePublicHistoryCacheAsync(outputCacheStore, publicQueryCache, cancellationToken);
        TempData[MessageKey] = !isPublished ? "Timeline event published." : "Timeline event unpublished.";
        TempData[MessageKindKey] = "success";
        return Redirect("/admin/timeline");
    }

    internal static async Task InvalidatePublicHistoryCacheAsync(
        IOutputCacheStore outputCacheStore,
        PublicQueryCacheService publicQueryCache,
        CancellationToken cancellationToken)
    {
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
        publicQueryCache.InvalidateHistoryCache();
    }

    private AdminQueenHistoryListFilter BuildFilter()
    {
        bool? isPublished = Published?.ToLowerInvariant() switch
        {
            "published" => true,
            "unpublished" => false,
            _ => null,
        };

        return new AdminQueenHistoryListFilter(isPublished, Q);
    }
}
