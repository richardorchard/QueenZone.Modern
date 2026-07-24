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
    IPhotoRepository photoRepository)
{
    private static readonly MemoryCacheEntryOptions VersionEntryOptions = new()
    {
        Priority = CacheItemPriority.NeverRemove
    };

    /// <summary>
    /// Per-key gates so concurrent cold-cache hits share a single factory execution (no stampede).
    /// Key set is small (news version variants, forum stats, on-this-day dates, photo pages).
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> loadGates = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<NewsItem>> GetLatestNewsAsync(int count, CancellationToken cancellationToken = default)
    {
        var version = GetNewsCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.LatestNews(version, count),
            options.Value.NewsCacheDuration,
            () => newsRepository.GetLatestAsync(count, cancellationToken));
    }

    public Task<int> GetNewsPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var version = GetNewsCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.NewsPublishedCount(version),
            options.Value.NewsCacheDuration,
            () => newsRepository.GetPublishedCountAsync(cancellationToken));
    }

    public Task<int> GetArticlePublishedCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ArticlePublishedCount,
            options.Value.ArticleCountCacheDuration,
            () => articlesRepository.GetPublishedCountAsync(cancellationToken));

    public Task<IReadOnlyList<ForumCategoryItem>> GetForumCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ForumCategories,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetCategoriesAsync(cancellationToken));

    public Task<int> GetForumThreadCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.ForumThreadCount,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetTotalThreadCountAsync(cancellationToken));

    public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.OnThisDay(date, count),
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetOnThisDayAsync(date, count, cancellationToken));

    public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            PublicQueryCacheKeys.AroundThisDay(date, dayWindow, count),
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetAroundThisDayAsync(date, dayWindow, count, cancellationToken));

    public Task<IReadOnlyList<PhotoCategory>> GetPhotoCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var version = GetPhotoCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.PhotoCategories(version),
            options.Value.PhotoCacheDuration,
            () => photoRepository.GetCategoriesAsync(cancellationToken));
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
        CancellationToken cancellationToken = default)
    {
        var version = GetPhotoCacheVersion();
        return GetOrCreateAsync(
            PublicQueryCacheKeys.PhotoCategoryPage(version, catId, page, pageSize),
            options.Value.PhotoCacheDuration,
            () => photoRepository.GetCategoryPageAsync(catId, page, pageSize, cancellationToken));
    }

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
    }

    /// <summary>
    /// Bumps the photo cache version so category lists and paged grids refresh after admin writes.
    /// </summary>
    public void InvalidatePhotoCache()
    {
        cache.Set(PublicQueryCacheKeys.PhotoVersion, CreateCacheVersion(), VersionEntryOptions);
    }

    private string GetNewsCacheVersion() => GetOrInitVersion(PublicQueryCacheKeys.NewsVersion);

    private string GetPhotoCacheVersion() => GetOrInitVersion(PublicQueryCacheKeys.PhotoVersion);

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
        Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var gate = loadGates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);
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
