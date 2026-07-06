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
    IQueenHistoryRepository queenHistoryRepository)
{
    private const string LatestNewsKeyPrefix = "public-query:news:latest";
    private const string NewsPublishedCountKey = "public-query:news:published-count";
    private const string ArticlePublishedCountKey = "public-query:articles:published-count";
    private const string ForumCategoriesKey = "public-query:forum:categories";
    private const string ForumThreadCountKey = "public-query:forum:thread-count";

    public Task<IReadOnlyList<NewsItem>> GetLatestNewsAsync(int count, CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            $"{LatestNewsKeyPrefix}:{count}",
            options.Value.NewsCacheDuration,
            () => newsRepository.GetLatestAsync(count, cancellationToken));

    public Task<int> GetNewsPublishedCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            NewsPublishedCountKey,
            options.Value.NewsCacheDuration,
            () => newsRepository.GetPublishedCountAsync(cancellationToken));

    public Task<int> GetArticlePublishedCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            ArticlePublishedCountKey,
            options.Value.ArticleCountCacheDuration,
            () => articlesRepository.GetPublishedCountAsync(cancellationToken));

    public Task<IReadOnlyList<ForumCategoryItem>> GetForumCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            ForumCategoriesKey,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetCategoriesAsync(cancellationToken));

    public Task<int> GetForumThreadCountAsync(CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            ForumThreadCountKey,
            options.Value.ForumStatsCacheDuration,
            () => forumRepository.GetTotalThreadCountAsync(cancellationToken));

    public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            $"public-query:history:on-this-day:{date:yyyyMMdd}:{count}",
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetOnThisDayAsync(date, count, cancellationToken));

    public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default) =>
        GetOrCreateAsync(
            $"public-query:history:around-this-day:{date:yyyyMMdd}:{dayWindow}:{count}",
            options.Value.OnThisDayCacheDuration,
            () => queenHistoryRepository.GetAroundThisDayAsync(date, dayWindow, count, cancellationToken));

    public void InvalidateNewsCache()
    {
        cache.Remove(NewsPublishedCountKey);
        // Homepage currently asks for five latest news items. Keep the count in the key so
        // future callers can add more variants without changing the invalidation shape.
        cache.Remove($"{LatestNewsKeyPrefix}:5");
    }

    private async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan duration,
        Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
        {
            return cached;
        }

        var value = await factory();
        cache.Set(key, value, duration);
        return value;
    }
}
