using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class ActionModel(
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    INewsDiscoveryRepository discoveryRepository,
    PublicQueryCacheService publicQueryCache,
    CoreSitemapService coreSitemapService,
    IOutputCacheStore outputCacheStore,
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

        await adminNewsRepository.PublishAsync(id, EditorEmail, cancellationToken);
        await InvalidatePublicNewsCachesAsync(cancellationToken);
        await auditRepository.AppendAsync(id, "publish", EditorEmail, $"Published \"{article.Title}\"", cancellationToken);
        return Redirect("/admin/news");
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return ArticleNotFound(id);
        }

        await adminNewsRepository.UnpublishAsync(id, EditorEmail, cancellationToken);
        await InvalidatePublicNewsCachesAsync(cancellationToken);
        await auditRepository.AppendAsync(id, "unpublish", EditorEmail, $"Unpublished \"{article.Title}\"", cancellationToken);
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
        catch (Exception)
        {
            // Best-effort cleanup when discovery tables are unavailable or unmigrated.
        }

        try
        {
            await auditRepository.AppendAsync(id, "delete", EditorEmail, $"Deleted \"{article.Title}\"", cancellationToken);
            await adminNewsRepository.DeleteAsync(id, EditorEmail, cancellationToken);
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
