using Microsoft.Extensions.Logging;
using QueenZone.Data;
using QueenZone.Search.Shared;

namespace QueenZone.Web.Search;

/// <summary>
/// Best-effort incremental search-index updates for modern forum threads. Create and reply
/// write paths upsert the title-oriented thread document (same mapping as
/// <see cref="SearchReindexBuilder.MapForumThread"/>) so live titles and last-activity
/// timestamps do not wait for the scheduled reindex (#527). Failures are logged and
/// swallowed — stale search is the product risk, not a failed member write.
/// </summary>
/// <remarks>
/// Blank titles are removed rather than upserted, matching
/// <c>ModernForum_GetTopicSitemapPage</c>. There is currently no thread rename or
/// thread-level soft-delete write path; #527 remains the reconciliation backstop.
/// </remarks>
public sealed class ForumSearchIndexSynchronizer(
    ISearchIndexService searchIndexService,
    ILogger<ForumSearchIndexSynchronizer> logger)
{
    public async Task UpsertThreadAsync(
        int topicId,
        string title,
        DateTimeOffset? lastActivityAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                await searchIndexService.RemoveAsync(
                    SearchReindexBuilder.ForumThreadSourceKey(topicId),
                    cancellationToken);
                return;
            }

            await searchIndexService.UpsertAsync(
                SearchReindexBuilder.MapForumThread(new ForumTopicSitemapItem(
                    topicId,
                    title.Trim(),
                    lastActivityAt?.UtcDateTime)),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Best-effort search index upsert failed for forum thread {TopicId}",
                topicId);
        }
    }

    public async Task RemoveThreadAsync(int topicId, CancellationToken cancellationToken = default)
    {
        try
        {
            await searchIndexService.RemoveAsync(
                SearchReindexBuilder.ForumThreadSourceKey(topicId),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Best-effort search index removal failed for forum thread {TopicId}",
                topicId);
        }
    }
}
