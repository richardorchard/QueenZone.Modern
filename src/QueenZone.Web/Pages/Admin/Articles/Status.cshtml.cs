using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class StatusModel(IEditorialArticleRepository articles, IArticleSubmissionRepository submissions, PublicQueryCacheService cache, IOutputCacheStore outputCache, CoreSitemapService sitemap, NewsArticleImageService imageService) : AdminArticlesPageModel
{
    public async Task<IActionResult> OnPostAsync(Guid id, string status, CancellationToken ct)
    {
        if (status is not (EditorialArticleStatus.Published or EditorialArticleStatus.Unpublished)) return BadRequest();
        var existing = await articles.GetAsync(id, ct);
        if (existing is null) return NotFound();
        var updated = await articles.SetStatusAsync(id, status, EditorEmail, ct);
        if (updated is null) return NotFound();
        if (updated.SourceSubmissionId is Guid submissionId)
        {
            await submissions.UpdateStatusAsync(submissionId,
                status == EditorialArticleStatus.Published ? ArticleSubmissionStatus.Published : ArticleSubmissionStatus.ApprovedForPublishing,
                EditorEmail, null, null, updated.Slug, updated.Excerpt, updated.Tags, ct);
        }
        if (status == EditorialArticleStatus.Published)
        {
            await imageService.TryDeletePreviousUgcArticlesAsync(existing.PublishedImageBlobKey, updated.ImageBlobKey, ct);
        }
        cache.InvalidateArticlesCache();
        await sitemap.InvalidateAsync(ct);
        await outputCache.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, ct);
        return Redirect($"/admin/articles/editor/{id}");
    }
}
