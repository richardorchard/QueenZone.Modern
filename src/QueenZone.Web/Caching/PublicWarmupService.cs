namespace QueenZone.Web;

/// <summary>
/// Primes process-local public query caches for <c>/warmup</c>.
/// Each step runs in its own DI scope so EF repositories do not share a
/// <c>QueenZoneDbContext</c> under <see cref="Task.WhenAll"/> (same pattern as
/// <see cref="AdminDashboardService"/>; issues #322 / #335).
/// </summary>
public sealed class PublicWarmupService
{
    internal static readonly TimeSpan DefaultStepTimeout = TimeSpan.FromSeconds(8);

    internal const int StepCount = 10;

    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PublicWarmupService> logger;
    private readonly TimeSpan stepTimeout;

    public PublicWarmupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<PublicWarmupService> logger)
        : this(scopeFactory, timeProvider, logger, DefaultStepTimeout)
    {
    }

    internal PublicWarmupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<PublicWarmupService> logger,
        TimeSpan stepTimeout)
    {
        this.scopeFactory = scopeFactory;
        this.timeProvider = timeProvider;
        this.logger = logger;
        this.stepTimeout = stepTimeout;
    }

    public Task WarmPublicCachesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        return Task.WhenAll(
            WarmStepAsync(
                "latest-news",
                (cache, stepToken) => cache.GetLatestNewsAsync(5, stepToken),
                cancellationToken),
            WarmStepAsync(
                "news-count",
                (cache, stepToken) => cache.GetNewsPublishedCountAsync(stepToken),
                cancellationToken),
            WarmStepAsync(
                "article-count",
                (cache, stepToken) => cache.GetArticlePublishedCountAsync(stepToken),
                cancellationToken),
            WarmStepAsync(
                "latest-articles",
                (cache, stepToken) => cache.GetLatestArticlesAsync(ArticlesRoutes.HomeFeaturedCount, stepToken),
                cancellationToken),
            WarmStepAsync(
                "forum-categories",
                (cache, stepToken) => cache.GetForumCategoriesAsync(stepToken),
                cancellationToken),
            WarmStepAsync(
                "forum-thread-count",
                (cache, stepToken) => cache.GetForumThreadCountAsync(stepToken),
                cancellationToken),
            WarmStepAsync(
                "forum-recent-threads",
                (cache, stepToken) => cache.GetForumRecentThreadsAsync(ForumRoutes.RecentThreadsCount, stepToken),
                cancellationToken),
            WarmStepAsync(
                "on-this-day",
                (cache, stepToken) => cache.GetOnThisDayAsync(today, 3, stepToken),
                cancellationToken),
            WarmStepAsync(
                "around-this-day",
                (cache, stepToken) => cache.GetAroundThisDayAsync(today, 7, 3, stepToken),
                cancellationToken),
            WarmStepAsync(
                "photo-categories",
                (cache, stepToken) => cache.GetPhotoCategoriesAsync(stepToken),
                cancellationToken));
    }

    private async Task WarmStepAsync(
        string stepName,
        Func<PublicQueryCacheService, CancellationToken, Task> warmStep,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var publicQueryCache = scope.ServiceProvider.GetRequiredService<PublicQueryCacheService>();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(stepTimeout);

        try
        {
            await warmStep(publicQueryCache, timeout.Token);
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
