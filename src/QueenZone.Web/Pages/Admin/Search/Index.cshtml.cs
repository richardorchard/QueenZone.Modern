using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Web.Search;

namespace QueenZone.Web.Pages.Admin.Search;

/// <summary>
/// Lets an admin manually trigger a full <c>SearchDocument</c> reindex. Standing in for the
/// scheduled reindex worker (tracked separately) — without it, an admin has no way to backfill
/// the search index after deploying, or to recover if a write-path's best-effort immediate
/// index hook silently failed.
/// </summary>
public sealed class IndexModel(
    ISearchIndexService searchIndexService,
    SearchReindexBuilder reindexBuilder,
    ILogger<IndexModel> logger) : PageModel
{
    private const string MessageKey = "SearchIndexMessage";
    private const string MessageKindKey = "SearchIndexMessageKind";

    public IReadOnlyDictionary<string, int> ContentTypeCounts { get; private set; } =
        new Dictionary<string, int>();

    public int TotalCount { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData[MessageKey] as string;
        StatusMessageKind = TempData[MessageKindKey] as string;
        await LoadCountsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostReindexAsync(CancellationToken cancellationToken)
    {
        try
        {
            await reindexBuilder.ReindexAllAsync(cancellationToken);
            TempData[MessageKey] = "Search index rebuilt.";
            TempData[MessageKindKey] = "success";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Manual search reindex failed");
            TempData[MessageKey] = "Reindex failed — see application logs for details.";
            TempData[MessageKindKey] = "error";
        }

        return RedirectToPage();
    }

    private async Task LoadCountsAsync(CancellationToken cancellationToken)
    {
        ContentTypeCounts = await searchIndexService.GetContentTypeCountsAsync(cancellationToken);
        TotalCount = ContentTypeCounts.Values.Sum();
    }
}
