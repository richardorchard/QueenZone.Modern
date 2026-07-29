namespace QueenZone.Web;

public sealed class PublicWarmupService
{
    internal static readonly TimeSpan DefaultStepTimeout = TimeSpan.FromSeconds(8);

    private readonly PublicQueryCacheService publicQueryCache;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PublicWarmupService> logger;
    private readonly TimeSpan stepTimeout;

    public PublicWarmupService(
        PublicQueryCacheService publicQueryCache,
        TimeProvider timeProvider,
        ILogger<PublicWarmupService> logger)
        : this(publicQueryCache, timeProvider, logger, DefaultStepTimeout)
    {
    }

    internal PublicWarmupService(
        PublicQueryCacheService publicQueryCache,
        TimeProvider timeProvider,
        ILogger<PublicWarmupService> logger,
        TimeSpan stepTimeout)
    {
        this.publicQueryCache = publicQueryCache;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.stepTimeout = stepTimeout;
    }

    public async Task WarmPublicCachesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        await WarmStepAsync(
            "latest-news",
            stepToken => publicQueryCache.GetLatestNewsAsync(5, stepToken),
            cancellationToken);
        await WarmStepAsync(
            "news-count",
            publicQueryCache.GetNewsPublishedCountAsync,
            cancellationToken);
        await WarmStepAsync(
            "article-count",
            publicQueryCache.GetArticlePublishedCountAsync,
            cancellationToken);
        await WarmStepAsync(
            "forum-categories",
            publicQueryCache.GetForumCategoriesAsync,
            cancellationToken);
        await WarmStepAsync(
            "forum-thread-count",
            publicQueryCache.GetForumThreadCountAsync,
            cancellationToken);
        await WarmStepAsync(
            "on-this-day",
            stepToken => publicQueryCache.GetOnThisDayAsync(today, 3, stepToken),
            cancellationToken);
        await WarmStepAsync(
            "around-this-day",
            stepToken => publicQueryCache.GetAroundThisDayAsync(today, 7, 3, stepToken),
            cancellationToken);
        await WarmStepAsync(
            "photo-categories",
            publicQueryCache.GetPhotoCategoriesAsync,
            cancellationToken);
    }

    private async Task WarmStepAsync<T>(
        string stepName,
        Func<CancellationToken, Task<T>> warmStep,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(stepTimeout);

        try
        {
            _ = await warmStep(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Public warmup timed out while priming {WarmupStep}.", stepName);
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
