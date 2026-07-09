using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PublicQueryCacheServiceTests
{
    [Fact]
    public async Task LatestNewsAndPublishedCountAreCachedUntilInvalidated()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var service = CreateService(memoryCache, newsRepository: newsRepository);

        var firstLatest = await service.GetLatestNewsAsync(5);
        var secondLatest = await service.GetLatestNewsAsync(5);
        var firstPublishedCount = await service.GetNewsPublishedCountAsync();
        var secondPublishedCount = await service.GetNewsPublishedCountAsync();

        Assert.Same(firstLatest, secondLatest);
        Assert.Equal(firstLatest[0].Title, secondLatest[0].Title);
        Assert.Equal(firstPublishedCount, secondPublishedCount);
        Assert.Equal(1, newsRepository.LatestCallCount);
        Assert.Equal(1, newsRepository.PublishedCountCallCount);

        service.InvalidateNewsCache();
        var thirdLatest = await service.GetLatestNewsAsync(5);
        var thirdPublishedCount = await service.GetNewsPublishedCountAsync();

        Assert.NotSame(firstLatest, thirdLatest);
        Assert.NotEqual(firstLatest[0].Title, thirdLatest[0].Title);
        Assert.NotEqual(firstPublishedCount, thirdPublishedCount);
        Assert.Equal(2, newsRepository.LatestCallCount);
        Assert.Equal(2, newsRepository.PublishedCountCallCount);
    }

    [Fact]
    public async Task ArchiveCountsAreCachedPerContentType()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var articlesRepository = new CountingArticlesRepository();
        var service = CreateService(
            memoryCache,
            newsRepository: newsRepository,
            articlesRepository: articlesRepository);

        await service.GetNewsPublishedCountAsync();
        await service.GetNewsPublishedCountAsync();
        await service.GetArticlePublishedCountAsync();
        await service.GetArticlePublishedCountAsync();

        Assert.Equal(1, newsRepository.PublishedCountCallCount);
        Assert.Equal(1, articlesRepository.PublishedCountCallCount);
    }

    [Fact]
    public async Task ForumCategoriesAndThreadCountAreCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var forumRepository = new CountingForumRepository();
        var service = CreateService(memoryCache, forumRepository: forumRepository);

        await service.GetForumCategoriesAsync();
        await service.GetForumCategoriesAsync();
        await service.GetForumThreadCountAsync();
        await service.GetForumThreadCountAsync();

        Assert.Equal(1, forumRepository.CategoriesCallCount);
        Assert.Equal(1, forumRepository.ThreadCountCallCount);
    }

    [Fact]
    public async Task InvalidateNewsCache_does_not_evict_forum_or_history_cache()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var forumRepository = new CountingForumRepository();
        var historyRepository = new CountingQueenHistoryRepository();
        var service = CreateService(
            memoryCache,
            newsRepository: newsRepository,
            forumRepository: forumRepository,
            historyRepository: historyRepository);

        await service.GetLatestNewsAsync(5);
        await service.GetForumCategoriesAsync();
        await service.GetForumThreadCountAsync();
        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);

        service.InvalidateNewsCache();

        await service.GetLatestNewsAsync(5);
        await service.GetForumCategoriesAsync();
        await service.GetForumThreadCountAsync();
        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);

        Assert.Equal(2, newsRepository.LatestCallCount);
        Assert.Equal(1, forumRepository.CategoriesCallCount);
        Assert.Equal(1, forumRepository.ThreadCountCallCount);
        Assert.Equal(1, historyRepository.OnThisDayCallCount);
        Assert.Equal(1, historyRepository.AroundThisDayCallCount);
    }

    [Fact]
    public async Task InvalidateNewsCache_does_not_evict_article_published_count_cache()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var articlesRepository = new CountingArticlesRepository();
        var service = CreateService(
            memoryCache,
            newsRepository: newsRepository,
            articlesRepository: articlesRepository);

        await service.GetNewsPublishedCountAsync();
        await service.GetArticlePublishedCountAsync();

        service.InvalidateNewsCache();

        await service.GetNewsPublishedCountAsync();
        await service.GetArticlePublishedCountAsync();

        Assert.Equal(2, newsRepository.PublishedCountCallCount);
        Assert.Equal(1, articlesRepository.PublishedCountCallCount);
    }

    [Fact]
    public async Task InvalidateNewsCache_evicts_all_latest_count_variants_not_just_homepage_default()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var service = CreateService(memoryCache, newsRepository: newsRepository);

        await service.GetLatestNewsAsync(3);
        await service.GetLatestNewsAsync(5);
        await service.GetLatestNewsAsync(10);
        await service.GetNewsPublishedCountAsync();

        Assert.Equal(3, newsRepository.LatestCallCount);
        Assert.Equal(1, newsRepository.PublishedCountCallCount);

        service.InvalidateNewsCache();

        await service.GetLatestNewsAsync(3);
        await service.GetLatestNewsAsync(5);
        await service.GetLatestNewsAsync(10);
        await service.GetNewsPublishedCountAsync();

        Assert.Equal(6, newsRepository.LatestCallCount);
        Assert.Equal(2, newsRepository.PublishedCountCallCount);
    }

    [Fact]
    public async Task LatestNewsCacheKeys_are_isolated_by_count_until_version_bump()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CountingNewsRepository();
        var service = CreateService(memoryCache, newsRepository: newsRepository);

        var latest3 = await service.GetLatestNewsAsync(3);
        var latest5 = await service.GetLatestNewsAsync(5);
        await service.GetLatestNewsAsync(3);
        await service.GetLatestNewsAsync(5);

        Assert.Equal(2, newsRepository.LatestCallCount);
        Assert.NotSame(latest3, latest5);

        service.InvalidateNewsCache();
        var latest3After = await service.GetLatestNewsAsync(3);

        Assert.Equal(3, newsRepository.LatestCallCount);
        Assert.NotSame(latest3, latest3After);
    }

    [Fact]
    public async Task OnThisDayCacheVariesByDateAndCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var historyRepository = new CountingQueenHistoryRepository();
        var service = CreateService(memoryCache, historyRepository: historyRepository);

        var july6Count3First = await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        var july6Count3Second = await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        var july7Count3 = await service.GetOnThisDayAsync(new DateOnly(2026, 7, 7), 3);
        var july6Count4 = await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 4);
        var aroundFirst = await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);
        var aroundSecond = await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);

        Assert.Same(july6Count3First, july6Count3Second);
        Assert.Same(aroundFirst, aroundSecond);
        Assert.NotSame(july6Count3First, july7Count3);
        Assert.NotSame(july6Count3First, july6Count4);
        Assert.Equal("on-this-day:2026-07-06:3", july6Count3First[0].Title);
        Assert.Equal("on-this-day:2026-07-07:3", july7Count3[0].Title);
        Assert.Equal("on-this-day:2026-07-06:4", july6Count4[0].Title);
        Assert.Equal(3, historyRepository.OnThisDayCallCount);
        Assert.Equal(1, historyRepository.AroundThisDayCallCount);
    }

    private static PublicQueryCacheService CreateService(
        IMemoryCache memoryCache,
        INewsRepository? newsRepository = null,
        IArticlesRepository? articlesRepository = null,
        IForumRepository? forumRepository = null,
        IQueenHistoryRepository? historyRepository = null) =>
        new(
            memoryCache,
            Options.Create(new PublicQueryCacheOptions()),
            newsRepository ?? new CountingNewsRepository(),
            articlesRepository ?? new CountingArticlesRepository(),
            forumRepository ?? new CountingForumRepository(),
            historyRepository ?? new CountingQueenHistoryRepository());

    private sealed class CountingNewsRepository : INewsRepository
    {
        public int LatestCallCount { get; private set; }

        public int PublishedCountCallCount { get; private set; }

        public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
        {
            LatestCallCount++;
            var item = new NewsItem(
                1,
                $"Cached news {LatestCallCount}",
                "Cached news excerpt.",
                "Cached news body.",
                new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
                null,
                true);
            return Task.FromResult<IReadOnlyList<NewsItem>>([item]);
        }

        public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            PublishedCountCallCount++;
            return Task.FromResult(PublishedCountCallCount);
        }

        public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewsItem>>([]);

        public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<NewsItem?>(null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([]);
    }

    private sealed class CountingArticlesRepository : IArticlesRepository
    {
        private readonly ArticleItem item = new(
            1,
            "Cached article",
            "Cached article excerpt.",
            "Cached article body.",
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
            null,
            null,
            true);

        public int PublishedCountCallCount { get; private set; }

        public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            PublishedCountCallCount++;
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArticleItem>>([item]);

        public Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArticleItem>>([item]);

        public Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArticleItem?>(id == item.Id ? item : null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([new SitemapContentEntry(item.Id, item.Title, item.PublishedAt)]);
    }

    private sealed class CountingForumRepository : IForumRepository
    {
        private readonly ForumCategoryItem category = new(
            1,
            "Cached forum",
            "Cached forum description.",
            12,
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
            "Cached thread",
            1);

        public int CategoriesCallCount { get; private set; }

        public int ThreadCountCallCount { get; private set; }

        public Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            CategoriesCallCount++;
            return Task.FromResult<IReadOnlyList<ForumCategoryItem>>([category]);
        }

        public Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default)
        {
            ThreadCountCallCount++;
            return Task.FromResult(4);
        }

        public Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ForumArchiveStats(1, 4, 12));

        public Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ForumCategoryItem?>(id == category.Id ? category : null);

        public Task<ForumCategoryTopicsPage> GetCategoryTopicsPageAsync(int forumId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ForumTopicPostsPage?> GetTopicPostsPageAsync(int topicId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetTopicSitemapCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<ForumTopicSitemapItem>> GetTopicSitemapPageAsync(int offset, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ForumTopicSitemapItem>>([]);

        public Task<ForumSearchPage> SearchForumAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CountingQueenHistoryRepository : IQueenHistoryRepository
    {
        public int OnThisDayCallCount { get; private set; }

        public int AroundThisDayCallCount { get; private set; }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(DateOnly date, int count, CancellationToken cancellationToken = default)
        {
            OnThisDayCallCount++;
            return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([CreateEvent($"on-this-day:{date:yyyy-MM-dd}:{count}")]);
        }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
            DateOnly date,
            int dayWindow,
            int count,
            CancellationToken cancellationToken = default)
        {
            AroundThisDayCallCount++;
            return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>(
                [CreateEvent($"around-this-day:{date:yyyy-MM-dd}:{dayWindow}:{count}")]);
        }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetAllPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([CreateEvent("all-published")]);

        private static QueenHistoryEvent CreateEvent(string title) =>
            new(
                1,
                title,
                "Cached history summary.",
                new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc),
                QueenHistoryDatePrecision.ExactDate,
                QueenHistoryEventCategory.Concert,
                100,
                QueenHistoryEventSourceType.Curated,
                "cached-history",
                null,
                true);
    }
}
