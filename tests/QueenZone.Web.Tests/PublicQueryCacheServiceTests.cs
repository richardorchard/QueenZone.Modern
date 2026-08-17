using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
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
        await service.GetForumRecentThreadsAsync(ForumRoutes.RecentThreadsCount);
        await service.GetForumRecentThreadsAsync(ForumRoutes.RecentThreadsCount);

        Assert.Equal(1, forumRepository.CategoriesCallCount);
        Assert.Equal(1, forumRepository.ThreadCountCallCount);
        Assert.Equal(1, forumRepository.RecentThreadsCallCount);
    }

    [Fact]
    public async Task InvalidateForumStatsCache_evicts_categories_thread_count_and_recent_threads()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var forumRepository = new CountingForumRepository();
        var service = CreateService(memoryCache, forumRepository: forumRepository);

        await service.GetForumCategoriesAsync();
        await service.GetForumThreadCountAsync();
        await service.GetForumRecentThreadsAsync(ForumRoutes.RecentThreadsCount);

        service.InvalidateForumStatsCache();

        await service.GetForumCategoriesAsync();
        await service.GetForumThreadCountAsync();
        await service.GetForumRecentThreadsAsync(ForumRoutes.RecentThreadsCount);

        Assert.Equal(2, forumRepository.CategoriesCallCount);
        Assert.Equal(2, forumRepository.ThreadCountCallCount);
        Assert.Equal(2, forumRepository.RecentThreadsCallCount);
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
    public async Task InvalidateArticleCountCache_evicts_article_published_count_only()
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
        await service.GetArticlePublishedCountAsync();
        Assert.Equal(1, articlesRepository.PublishedCountCallCount);

        service.InvalidateArticleCountCache();

        await service.GetArticlePublishedCountAsync();
        await service.GetNewsPublishedCountAsync();

        Assert.Equal(2, articlesRepository.PublishedCountCallCount);
        Assert.Equal(1, newsRepository.PublishedCountCallCount);
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
    public async Task Concurrent_cold_cache_hits_invoke_factory_once()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new SlowCountingNewsRepository(TimeSpan.FromMilliseconds(100));
        var service = CreateService(memoryCache, newsRepository: newsRepository);

        var tasks = Enumerable.Range(0, 12)
            .Select(_ => service.GetLatestNewsAsync(5))
            .ToArray();
        await Task.WhenAll(tasks);

        Assert.Equal(1, newsRepository.LatestCallCount);
        Assert.All(tasks, t => Assert.Same(tasks[0].Result, t.Result));
    }

    [Fact]
    public async Task Concurrent_cold_cache_hits_across_scoped_instances_invoke_factory_once()
    {
        // Production registers PublicQueryCacheService as scoped; gates must be process-wide.
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new SlowCountingNewsRepository(TimeSpan.FromMilliseconds(100));
        var services = Enumerable.Range(0, 12)
            .Select(_ => CreateService(memoryCache, newsRepository: newsRepository))
            .ToArray();

        var tasks = services.Select(s => s.GetLatestNewsAsync(5)).ToArray();
        await Task.WhenAll(tasks);

        Assert.Equal(1, newsRepository.LatestCallCount);
        Assert.All(tasks, t => Assert.Same(tasks[0].Result, t.Result));
    }

    [Fact]
    public async Task Waiting_for_busy_cache_key_observes_cancellation()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new BlockingNewsRepository();
        var service = CreateService(memoryCache, newsRepository: newsRepository);

        var first = service.GetLatestNewsAsync(5);
        await newsRepository.WaitUntilEnteredAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetLatestNewsAsync(5, timeout.Token));

        newsRepository.Release();
        _ = await first;
    }

    [Fact]
    public async Task PublicWarmupService_times_out_slow_cache_prime()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var newsRepository = new CancellationBlockingNewsRepository();
        var cache = CreateService(memoryCache, newsRepository: newsRepository);
        await using var provider = CreateWarmupProvider(cache);
        var warmup = new PublicWarmupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<PublicWarmupService>.Instance,
            TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            warmup.WarmPublicCachesAsync());

        await newsRepository.WaitUntilEnteredAsync();
    }

    [Fact]
    public async Task PublicWarmupService_primes_cache_steps_concurrently()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var gate = new ConcurrentEntryGate(PublicWarmupService.StepCount);
        var cache = CreateService(
            memoryCache,
            newsRepository: new BarrierNewsRepository(gate),
            articlesRepository: new BarrierArticlesRepository(gate),
            forumRepository: new BarrierForumRepository(gate),
            historyRepository: new BarrierQueenHistoryRepository(gate),
            photoRepository: new BarrierPhotoRepository(gate));
        await using var provider = CreateWarmupProvider(cache);
        var warmup = new PublicWarmupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<PublicWarmupService>.Instance,
            TimeSpan.FromSeconds(2));

        await warmup.WarmPublicCachesAsync();

        Assert.Equal(PublicWarmupService.StepCount, gate.Entered);
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

    [Fact]
    public async Task PhotoCategoriesAndPagesAreCachedUntilInvalidated()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var photoRepository = new CountingPhotoRepository();
        var service = CreateService(memoryCache, photoRepository: photoRepository);

        var firstCategories = await service.GetPhotoCategoriesAsync();
        var secondCategories = await service.GetPhotoCategoriesAsync();
        var firstPage = await service.GetPhotoCategoryPageAsync(9, 1, 24);
        var secondPage = await service.GetPhotoCategoryPageAsync(9, 1, 24);

        Assert.Same(firstCategories, secondCategories);
        Assert.Same(firstPage, secondPage);
        Assert.Equal(1, photoRepository.CategoriesCallCount);
        Assert.Equal(1, photoRepository.PageCallCount);

        service.InvalidatePhotoCache();

        _ = await service.GetPhotoCategoriesAsync();
        _ = await service.GetPhotoCategoryPageAsync(9, 1, 24);

        Assert.Equal(2, photoRepository.CategoriesCallCount);
        Assert.Equal(2, photoRepository.PageCallCount);
    }

    private static ServiceProvider CreateWarmupProvider(PublicQueryCacheService cache)
    {
        var services = new ServiceCollection();
        services.AddSingleton(cache);
        return services.BuildServiceProvider();
    }

    private static PublicQueryCacheService CreateService(
        IMemoryCache memoryCache,
        INewsRepository? newsRepository = null,
        IArticlesRepository? articlesRepository = null,
        IForumRepository? forumRepository = null,
        IQueenHistoryRepository? historyRepository = null,
        IPhotoRepository? photoRepository = null) =>
        new(
            memoryCache,
            Options.Create(new PublicQueryCacheOptions()),
            newsRepository ?? new CountingNewsRepository(),
            articlesRepository ?? new CountingArticlesRepository(),
            forumRepository ?? new CountingForumRepository(),
            historyRepository ?? new CountingQueenHistoryRepository(),
            photoRepository ?? new CountingPhotoRepository());

    private sealed class SlowCountingNewsRepository(TimeSpan delay) : CountingNewsRepository
    {
        public override async Task<IReadOnlyList<NewsItem>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delay, cancellationToken);
            return await base.GetLatestAsync(count, cancellationToken);
        }
    }

    private sealed class BlockingNewsRepository : CountingNewsRepository
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilEnteredAsync() => entered.Task;

        public void Release() => release.TrySetResult();

        public override async Task<IReadOnlyList<NewsItem>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await release.Task;
            return await base.GetLatestAsync(count, cancellationToken);
        }
    }

    private sealed class CancellationBlockingNewsRepository : CountingNewsRepository
    {
        private readonly TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitUntilEnteredAsync() => entered.Task;

        public override async Task<IReadOnlyList<NewsItem>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return await base.GetLatestAsync(count, cancellationToken);
        }
    }

    private class CountingNewsRepository : INewsRepository
    {
        public int LatestCallCount { get; private set; }

        public int PublishedCountCallCount { get; private set; }

        public virtual Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
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

        public virtual Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
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

        public Task<NewsSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsSearchPage([], 0, page, pageSize));
    }

    private class CountingArticlesRepository : IArticlesRepository
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

        public virtual Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            PublishedCountCallCount++;
            return Task.FromResult(1);
        }

        public virtual Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArticleItem>>([item]);

        public Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ArticleItem>>([item]);

        public Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArticleItem?>(id == item.Id ? item : null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([new SitemapContentEntry(item.Id, item.Title, item.PublishedAt)]);
    }

    private class CountingForumRepository : IForumRepository
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

        public int RecentThreadsCallCount { get; private set; }

        public virtual Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            CategoriesCallCount++;
            return Task.FromResult<IReadOnlyList<ForumCategoryItem>>([category]);
        }

        public virtual Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default)
        {
            ThreadCountCallCount++;
            return Task.FromResult(4);
        }

        public virtual Task<IReadOnlyList<ForumRecentThreadItem>> GetRecentThreadsAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            RecentThreadsCallCount++;
            return Task.FromResult<IReadOnlyList<ForumRecentThreadItem>>([
                new ForumRecentThreadItem(
                    1001,
                    "Cached recent thread",
                    category.Id,
                    category.Name,
                    3,
                    category.LastActivityAt ?? DateTime.UtcNow)
            ]);
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

    private class CountingQueenHistoryRepository : IQueenHistoryRepository
    {
        public int OnThisDayCallCount { get; private set; }

        public int AroundThisDayCallCount { get; private set; }

        public virtual Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(DateOnly date, int count, CancellationToken cancellationToken = default)
        {
            OnThisDayCallCount++;
            return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>([CreateEvent($"on-this-day:{date:yyyy-MM-dd}:{count}")]);
        }

        public virtual Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
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

    private class CountingPhotoRepository : IPhotoRepository
    {
        public int CategoriesCallCount { get; private set; }

        public int PageCallCount { get; private set; }

        public virtual Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            CategoriesCallCount++;
            return Task.FromResult<IReadOnlyList<PhotoCategory>>(
            [
                new PhotoCategory(9, "Brian May", "brian-may", 3, "https://cdn.queenzone.org/brian-may/cover.jpg"),
            ]);
        }

        public Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PhotoCategoryPage> GetCategoryPageAsync(
            int catId,
            int page,
            int pageSize,
            PhotoListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            PageCallCount++;
            return Task.FromResult(new PhotoCategoryPage(
                "Brian May",
                [
                    new PhotoItem(
                        101,
                        catId,
                        "Brian May",
                        "brian-may",
                        $"page-{page}",
                        "https://cdn.queenzone.org/brian-may/a.jpg",
                        "https://cdn.queenzone.org/brian-may/a-t.jpg",
                        150,
                        150,
                        1920,
                        1080,
                        1986,
                        new DateTime(1986, 7, 12)),
                ],
                3));
        }

        public Task<PhotoDetailNavigation?> GetDetailNavigationAsync(
            int catId,
            int picId,
            PhotoListFilter? filter = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PhotoItem>> GetRandomPublishedInCategoryAsync(
            int catId,
            int take,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PhotoSitemapCategory>> GetPublishedSitemapCategoriesAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ConcurrentEntryGate(int expected)
    {
        private readonly TaskCompletionSource allEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int entered;

        public int Entered => Volatile.Read(ref entered);

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref entered) == expected)
            {
                allEntered.TrySetResult();
            }

            await allEntered.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class BarrierNewsRepository(ConcurrentEntryGate gate) : CountingNewsRepository
    {
        public override async Task<IReadOnlyList<NewsItem>> GetLatestAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetLatestAsync(count, cancellationToken);
        }

        public override async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetPublishedCountAsync(cancellationToken);
        }
    }

    private sealed class BarrierArticlesRepository(ConcurrentEntryGate gate) : CountingArticlesRepository
    {
        public override async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetPublishedCountAsync(cancellationToken);
        }

        public override async Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetLatestAsync(count, cancellationToken);
        }
    }

    private sealed class BarrierForumRepository(ConcurrentEntryGate gate) : CountingForumRepository
    {
        public override async Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetCategoriesAsync(cancellationToken);
        }

        public override async Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetTotalThreadCountAsync(cancellationToken);
        }

        public override async Task<IReadOnlyList<ForumRecentThreadItem>> GetRecentThreadsAsync(
            int count,
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetRecentThreadsAsync(count, cancellationToken);
        }
    }

    private sealed class BarrierQueenHistoryRepository(ConcurrentEntryGate gate) : CountingQueenHistoryRepository
    {
        public override async Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
            DateOnly date,
            int count,
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetOnThisDayAsync(date, count, cancellationToken);
        }

        public override async Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
            DateOnly date,
            int dayWindow,
            int count,
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetAroundThisDayAsync(date, dayWindow, count, cancellationToken);
        }
    }

    private sealed class BarrierPhotoRepository(ConcurrentEntryGate gate) : CountingPhotoRepository
    {
        public override async Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            await gate.EnterAsync(cancellationToken);
            return await base.GetCategoriesAsync(cancellationToken);
        }
    }
}
