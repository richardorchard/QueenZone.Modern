namespace QueenZone.Web;

public sealed class PublicWarmupService(
    PublicQueryCacheService publicQueryCache,
    TimeProvider timeProvider,
    ILogger<PublicWarmupService> logger)
{
    public async Task WarmPublicCachesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        await WarmStepAsync(
            "latest-news",
            () => publicQueryCache.GetLatestNewsAsync(5, cancellationToken));
        await WarmStepAsync(
            "news-count",
            () => publicQueryCache.GetNewsPublishedCountAsync(cancellationToken));
        await WarmStepAsync(
            "article-count",
            () => publicQueryCache.GetArticlePublishedCountAsync(cancellationToken));
        await WarmStepAsync(
            "forum-categories",
            () => publicQueryCache.GetForumCategoriesAsync(cancellationToken));
        await WarmStepAsync(
            "forum-thread-count",
            () => publicQueryCache.GetForumThreadCountAsync(cancellationToken));
        await WarmStepAsync(
            "on-this-day",
            () => publicQueryCache.GetOnThisDayAsync(today, 3, cancellationToken));
        await WarmStepAsync(
            "around-this-day",
            () => publicQueryCache.GetAroundThisDayAsync(today, 7, 3, cancellationToken));
        await WarmStepAsync(
            "photo-categories",
            () => publicQueryCache.GetPhotoCategoriesAsync(cancellationToken));
    }

    private async Task WarmStepAsync<T>(string stepName, Func<Task<T>> warmStep)
    {
        try
        {
            _ = await warmStep();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            logger.LogWarning("Public warmup failed while priming {WarmupStep}.", stepName);
            throw;
        }
    }
}
