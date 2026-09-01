using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

public sealed class PublicQueryCacheService(
    IMemoryCache cache,
    IOptions<PublicQueryCacheOptions> options,
    INewsRepository newsRepository,
    IArticlesRepository articlesRepository,
    IForumRepository forumRepository,
    IQueenHistoryRepository queenHistoryRepository,
    IPhotoRepository photoRepository,
    ILiveActivityQueryService liveActivityQuery)
{
    private static readonly MemoryCacheEntryOptions VersionEntryOptions = new()
    {
        Priority = CacheItemPriority.NeverRemove
    };

    /// <summary>
    /// Process-wide per-key gates so concurrent cold-cache hits share a single factory execution
    /// even when <see cref="PublicQueryCacheService"/> is scoped (one instance per HTTP request).
    /// Key set is small (news version variants, forum stats, on-this-day dates, photo pages).
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> LoadGates =
        new(StringComparer.Ordinal);

    public Task<IReadOnlyList<NewsItem>> GetLatestNewsAsync(int count, CancellationToken cancellationToken = default)
    {
        var version = GetNewsCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.LatestNews(version, count),
            options.Value.NewsCacheDuration,
            () => newsRepository.GetLatestAsync(count, cancellationToken),
            cancellationToken);
    }

    public Task<int> GetNewsPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var version = GetNewsCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.NewsPublishedCount(version),
            options.Value.NewsCacheDuration,
            () => newsRepository.GetPublishedCountAsync(cancellationToken: cancellationToken),
            cancellationToken);
    }

    public Task<int> GetArticlePublishedCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ArticlePublishedCount,
            options.Value.ArticleCountCacheDuration,
            () => articlesRepository.GetPublishedCountAsync(cancellationToken),
            cancellationToken);

    public Task<IReadOnlyList<ArticleItem>> GetLatestArticlesAsync(int count, CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.LatestArticles(count),
            options.Value.ArticleCountCacheDuration,
            () => articlesRepository.GetLatestAsync(count, cancellationToken),
            cancellationToken);

    public Task<IReadOnlyList<ForumCategoryItem>> GetForumCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ForumCategories,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetCategoriesAsync(cancellationToken),
            cancellationToken);

    public Task<int> GetForumThreadCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ForumThreadCount,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetTotalThreadCountAsync(cancellationToken),
            cancellationToken);

    public Task<IReadOnlyList<ForumRecentThreadItem>> GetForumRecentThreadsAsync(
        int count,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ForumRecentThreads(count),
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetRecentThreadsAsync(count, cancellationToken),
            cancellationToken);

    public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default)
    {
        var version = GetHistoryCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.OnThisDay(version, date, count),
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetOnThisDayAsync(date, count, cancellationToken),
            cancellationToken);
    }

    public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default)
    {
        var version = GetHistoryCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.AroundThisDay(version, date, dayWindow, count),
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetAroundThisDayAsync(date, dayWindow, count, cancellationToken),
            cancellationToken);
    }

    public Task<IReadOnlyList<PhotoCategory>> GetPhotoCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var version = GetPhotoCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.PhotoCategories(version),
            options.Value.PhotoCacheDuration,
            () => photoRepository.GetCategoriesAsync(cancellationToken),
            cancellationToken);
    }

    public async Task<PhotoCategory?> GetPhotoCategoryBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var categories = await GetPhotoCategoriesAsync(cancellationToken);
        return categories.FirstOrDefault(category =>
            string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public Task<PhotoCategoryPage> GetPhotoCategoryPageAsync(
        int catId,
        int page,
        int pageSize,
        PhotoListFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        var activeFilter = filter ?? PhotoListFilter.None;
        var version = GetPhotoCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.PhotoCategoryPage(version, catId, page, pageSize, activeFilter.QueryValue),
            options.Value.PhotoCacheDuration,
            () => photoRepository.GetCategoryPageAsync(catId, page, pageSize, activeFilter, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// Count of forum posts made today. Short 45s TTL: no presence-tracking exists, so this
    /// is the only honest "live" signal for the mobile home screen's activity strip.
    /// </summary>
    public Task<int> GetLiveActivityNewForumRepliesTodayAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.LiveActivityNewForumReplies,
            options.Value.LiveActivityCacheDuration,
            () => liveActivityQuery.GetNewForumRepliesTodayAsync(cancellationToken),
            cancellationToken);

    /// <summary>
    /// Invalidates all public news cache entries (latest lists for any count and published count)
    /// by bumping the news cache version. Call after publish, unpublish, delete of published news,
    /// or edit of published news.
    /// </summary>
    public void InvalidateNewsCache()
    {
        // Versioned keys mean callers can introduce new latest-count variants without updating
        // invalidation. Previous version entries expire via their normal TTL.
        cache.Set(PublicQueryCacheKeys.NewsVersion, CreateCacheVersion(), VersionEntryOptions);
    }

    public void InvalidateForumStatsCache()
    {
        cache.Remove(PublicQueryCacheKeys.ForumCategories);
        cache.Remove(PublicQueryCacheKeys.ForumThreadCount);
        cache.Remove(PublicQueryCacheKeys.ForumRecentThreads(ForumRoutes.RecentThreadsCount));
    }

    /// <summary>
    /// Evicts the public legacy-article published count so archive pagination refreshes after
    /// editorial changes (or import tooling) that alter the published set.
    /// </summary>
    public void InvalidateArticleCountCache()
    {
        cache.Remove(PublicQueryCacheKeys.ArticlePublishedCount);
        cache.Remove(PublicQueryCacheKeys.LatestArticles(ArticlesRoutes.HomeFeaturedCount));
    }

    public void InvalidateArticlesCache()
    {
        cache.Remove(PublicQueryCacheKeys.LatestArticles(ArticlesRoutes.HomeFeaturedCount));
        InvalidateArticleCountCache();
    }

    /// <summary>
    /// Bumps the photo cache version so category lists and paged grids refresh after admin writes.
    /// </summary>
    public void InvalidatePhotoCache()
    {
        cache.Set(PublicQueryCacheKeys.PhotoVersion, CreateCacheVersion(), VersionEntryOptions);
    }

    public void InvalidateHistoryCache()
    {
        cache.Set(PublicQueryCacheKeys.HistoryVersion, CreateCacheVersion(), VersionEntryOptions);
    }

    private string GetNewsCacheVersion() => GetOrInitVersion(PublicQueryCacheKeys.NewsVersion);

    private string GetPhotoCacheVersion() => GetOrInitVersion(PublicQueryCacheKeys.PhotoVersion);

    private string GetHistoryCacheVersion() => GetOrInitVersion(PublicQueryCacheKeys.HistoryVersion);

    private string GetOrInitVersion(string key)
    {
        if (cache.TryGetValue(key, out string? version) && !string.IsNullOrEmpty(version))
        {
            return version;
        }

        var initial = "0";
        cache.Set(key, initial, VersionEntryOptions);
        return initial;
    }

    private static string CreateCacheVersion() => Guid.NewGuid().ToString("N");

    private async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan duration,
        Func<Task<T>> factory,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var gate = LoadGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached;
            }

            var value = await factory().ConfigureAwait(false);
            cache.Set(key, value, duration);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }
}
