using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Search;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class ActionModel(
    IAdminNewsRepository adminNewsRepository,
    AdminNewsWriteService adminNewsWriteService,
    INewsRepository newsRepository,
    INewsAuditRepository auditRepository,
    INewsDiscoveryRepository discoveryRepository,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore,
    ISearchIndexService searchIndexService,
    ILogger<ActionModel> logger) : AdminNewsPageModel
{
    public IActionResult OnGet(int id, string handler) =>
        Redirect("/admin/news");

    public async Task<IActionResult> OnPostPublishAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return ArticleNotFound(id);
        }

        var draft = ToDraft(article);
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(draft.Title, draft.Slug),
            excludeNewsId: id,
            cancellationToken: cancellationToken);
        var validationErrors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (validationErrors.Count > 0)
        {
            TempData[AdminNewsMessages.MessageKey] = string.Join(" ", validationErrors);
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect($"/admin/news/{id}/edit");
        }

        await adminNewsWriteService.PublishAsync(article, EditorEmail, cancellationToken);
        await InvalidatePublicNewsCachesAsync(cancellationToken);
        await auditRepository.AppendAsync(id, "publish", EditorEmail, $"Published \"{article.Title}\"", cancellationToken);
        await UpsertSearchIndexAsync(id, cancellationToken);
        return Redirect("/admin/news");
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return ArticleNotFound(id);
        }

        await adminNewsRepository.UnpublishAsync(id, EditorEmail, article.UpdatedAt, cancellationToken);
        await InvalidatePublicNewsCachesAsync(cancellationToken);
        await auditRepository.AppendAsync(id, "unpublish", EditorEmail, $"Unpublished \"{article.Title}\"", cancellationToken);
        await RemoveSearchIndexAsync(id, cancellationToken);
        return Redirect("/admin/news");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return ArticleNotFound(id);
        }

        try
        {
            await discoveryRepository.ClearPromotedNewsLinksAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort cleanup when discovery tables are unavailable or unmigrated.
            // Delete the news row even if provenance unlink fails; do not hide the failure from logs.
            logger.LogWarning(
                ex,
                "Best-effort discovery link cleanup failed for news article {NewsId}; continuing with delete",
                id);
        }

        try
        {
            await adminNewsRepository.DeleteAsync(id, EditorEmail, article.UpdatedAt, cancellationToken);
            await auditRepository.AppendAsync(id, "delete", EditorEmail, $"Deleted \"{article.Title}\"", cancellationToken);
            await RemoveSearchIndexAsync(id, cancellationToken);
            if (article.IsPublished)
            {
                await InvalidatePublicNewsCachesAsync(cancellationToken);
            }
        }
        catch (Exception ex) when (AdminNewsDeleteError.IsDeleteForeignKeyViolation(ex))
        {
            TempData[AdminNewsMessages.MessageKey] =
                "This article could not be deleted because other archive records still reference it. Unpublish it instead to hide it from the public site.";
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect("/admin/news");
        }
        catch (InvalidOperationException ex)
        {
            TempData[AdminNewsMessages.MessageKey] = ex.Message;
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect("/admin/news");
        }

        return Redirect("/admin/news");
    }

    private async Task UpsertSearchIndexAsync(int id, CancellationToken cancellationToken)
    {
        // Best-effort: the scheduled batch reindex is the correctness backstop if this fails
        // or if the published item can't be re-read (e.g. attribution lookup issues).
        try
        {
            var published = await newsRepository.GetByIdAsync(id, cancellationToken);
            if (published is not null)
            {
                await searchIndexService.UpsertAsync(SearchReindexBuilder.MapNews(published), cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Best-effort search index upsert failed for news article {NewsId}", id);
        }
    }

    private async Task RemoveSearchIndexAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            await searchIndexService.RemoveAsync($"news:{id}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Best-effort search index removal failed for news article {NewsId}", id);
        }
    }

    private async Task InvalidatePublicNewsCachesAsync(CancellationToken cancellationToken)
    {
        publicQueryCache.InvalidateNewsCache();
        await coreSitemapService.InvalidateAsync(cancellationToken);
        // Drop anonymous HTML output-cache entries so / and /news reflect publish actions immediately.
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }

    private IActionResult ArticleNotFound(int id)
    {
        logger.LogWarning("Admin news action requested for missing article {NewsId}", id);
        TempData[AdminNewsMessages.MessageKey] = $"News article {id} was not found.";
        TempData[AdminNewsMessages.MessageKindKey] = "error";
        return Redirect("/admin/news");
    }
}
