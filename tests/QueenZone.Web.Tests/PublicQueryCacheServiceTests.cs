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

        await service.GetLatestNewsAsync(5);
        await service.GetLatestNewsAsync(5);
        await service.GetNewsPublishedCountAsync();
        await service.GetNewsPublishedCountAsync();

        Assert.Equal(1, newsRepository.LatestCallCount);
        Assert.Equal(1, newsRepository.PublishedCountCallCount);

        service.InvalidateNewsCache();
        await service.GetLatestNewsAsync(5);
        await service.GetNewsPublishedCountAsync();

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
    public async Task OnThisDayCacheVariesByDateAndCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var historyRepository = new CountingQueenHistoryRepository();
        var service = CreateService(memoryCache, historyRepository: historyRepository);

        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 3);
        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 7), 3);
        await service.GetOnThisDayAsync(new DateOnly(2026, 7, 6), 4);
        await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);
        await service.GetAroundThisDayAsync(new DateOnly(2026, 7, 6), 7, 3);

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
        private readonly NewsItem item = new(
            1,
            "Cached news",
            "Cached news excerpt.",
            "Cached news body.",
            new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc),
            null,
            true);

        public int LatestCallCount { get; private set; }

        public int PublishedCountCallCount { get; private set; }

        public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
        {
            LatestCallCount++;
            return Task.FromResult<IReadOnlyList<NewsItem>>([item]);
        }

        public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            PublishedCountCallCount++;
            return Task.FromResult(1);
        }

        public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewsItem>>([item]);

        public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<NewsItem?>(id == item.Id ? item : null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([new SitemapContentEntry(item.Id, item.Title, item.PublishedAt)]);
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
        private readonly QueenHistoryEvent historyEvent = new(
            1,
            "Cached history",
            "Cached history summary.",
            new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Concert,
            100,
            QueenHistoryEventSourceType.Curated,
            "cached-history",
            null,
            true);

        public int OnThisDayCallCount { get; private set; }

        public int AroundThisDayCallCount { get; private set; }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(DateOnly date, int count, CancellationToken cancellationToken = default)
        {
            OnThisDayCallCount++;
            return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([historyEvent]);
        }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
            DateOnly date,
            int dayWindow,
            int count,
            CancellationToken cancellationToken = default)
        {
            AroundThisDayCallCount++;
            return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([historyEvent]);
        }

        public Task<IReadOnlyList<QueenHistoryEvent>> GetAllPublishedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([historyEvent]);
    }
}
