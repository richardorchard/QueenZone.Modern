using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsAgentGuidanceProviderTests
{
    [Fact]
    public void CacheDuration_is_sixty_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(60), NewsAgentGuidanceProvider.CacheDuration);
    }

    [Fact]
    public async Task GetPublishedAsync_caches_snapshot_with_absolute_sixty_second_expiration()
    {
        var store = new SharedNewsAgentGuidanceStore();
        var repository = new InMemoryNewsAgentGuidanceRepository(store);
        await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "prefer member-news", "admin@test.local", null);
        var draft = await repository.GetDraftAsync(NewsAgentGuidanceType.Triage);
        await repository.PublishDraftAsync(NewsAgentGuidanceType.Triage, "admin@test.local", draft!.RowVersion);

        var cache = new RecordingMemoryCache();
        var logger = new ListLogger<NewsAgentGuidanceProvider>();
        var provider = new NewsAgentGuidanceProvider(repository, cache, logger);

        var first = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);
        var second = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);

        Assert.Equal(first, second);
        Assert.Equal(1, cache.SetCount);
        Assert.Equal(TimeSpan.FromSeconds(60), cache.LastOptions?.AbsoluteExpirationRelativeToNow);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("prefer member-news", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetPublishedAsync_falls_back_when_repository_throws_without_logging_guidance_text()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new ListLogger<NewsAgentGuidanceProvider>();
        var provider = new NewsAgentGuidanceProvider(new ThrowingGuidanceRepository(), cache, logger);

        var snapshot = await provider.GetPublishedAsync(NewsAgentGuidanceType.Draft);

        Assert.Equal(NewsAgentGuidanceSnapshot.Empty, snapshot);
        Assert.Contains(logger.Messages, message => message.Contains("unavailable", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("draft", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("SECRET GUIDANCE TEXT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetPublishedAsync_falls_back_when_published_guidance_is_missing()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new ListLogger<NewsAgentGuidanceProvider>();
        var provider = new NewsAgentGuidanceProvider(
            new InMemoryNewsAgentGuidanceRepository(new SharedNewsAgentGuidanceStore()),
            cache,
            logger);

        var snapshot = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);

        Assert.Equal(NewsAgentGuidanceSnapshot.Empty, snapshot);
        Assert.Contains(logger.Messages, message => message.Contains("missing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Invalidate_forces_reload()
    {
        var store = new SharedNewsAgentGuidanceStore();
        var repository = new InMemoryNewsAgentGuidanceRepository(store);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new NewsAgentGuidanceProvider(
            repository,
            cache,
            new ListLogger<NewsAgentGuidanceProvider>());

        var before = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);
        Assert.False(before.HasRevision);

        var draft = await repository.SaveDraftAsync(NewsAgentGuidanceType.Triage, "new overlay", "admin@test.local", null);
        await repository.PublishDraftAsync(NewsAgentGuidanceType.Triage, "admin@test.local", draft.RowVersion);

        var cached = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);
        Assert.False(cached.HasOverlay);

        provider.Invalidate(NewsAgentGuidanceType.Triage);
        var after = await provider.GetPublishedAsync(NewsAgentGuidanceType.Triage);
        Assert.Equal("new overlay", after.Content);
        Assert.True(after.HasRevision);
    }

    private sealed class ThrowingGuidanceRepository : INewsAgentGuidanceRepository
    {
        public Task<NewsAgentGuidanceRevision?> GetPublishedAsync(
            NewsAgentGuidanceType type,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("SECRET GUIDANCE TEXT database down");

        public Task<NewsAgentGuidanceRevision?> GetDraftAsync(
            NewsAgentGuidanceType type,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsAgentGuidanceRevision?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NewsAgentGuidanceRevision>> ListHistoryAsync(
            NewsAgentGuidanceType type,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsAgentGuidanceRevision> SaveDraftAsync(
            NewsAgentGuidanceType type,
            string content,
            string editorEmail,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsAgentGuidanceRevision> PublishDraftAsync(
            NewsAgentGuidanceType type,
            string publisherEmail,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsAgentGuidanceRevision> RollbackAsync(
            NewsAgentGuidanceType type,
            int sourceRevisionId,
            string publisherEmail,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<NewsAgentGuidanceRevision> RestoreCompiledDefaultAsync(
            NewsAgentGuidanceType type,
            string publisherEmail,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingMemoryCache : IMemoryCache
    {
        private readonly MemoryCache inner = new(new MemoryCacheOptions());

        public int SetCount { get; private set; }

        public MemoryCacheEntryOptions? LastOptions { get; private set; }

        public ICacheEntry CreateEntry(object key)
        {
            var entry = inner.CreateEntry(key);
            return new RecordingCacheEntry(entry, options =>
            {
                SetCount++;
                LastOptions = options;
            });
        }

        public void Dispose() => inner.Dispose();

        public void Remove(object key) => inner.Remove(key);

        public bool TryGetValue(object key, out object? value) => inner.TryGetValue(key, out value);
    }

    private sealed class RecordingCacheEntry(ICacheEntry inner, Action<MemoryCacheEntryOptions> onDispose) : ICacheEntry
    {
        public object Key => inner.Key;

        public object? Value
        {
            get => inner.Value;
            set => inner.Value = value;
        }

        public DateTimeOffset? AbsoluteExpiration
        {
            get => inner.AbsoluteExpiration;
            set => inner.AbsoluteExpiration = value;
        }

        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => inner.AbsoluteExpirationRelativeToNow;
            set => inner.AbsoluteExpirationRelativeToNow = value;
        }

        public TimeSpan? SlidingExpiration
        {
            get => inner.SlidingExpiration;
            set => inner.SlidingExpiration = value;
        }

        public IList<IChangeToken> ExpirationTokens => inner.ExpirationTokens;

        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => inner.PostEvictionCallbacks;

        public CacheItemPriority Priority
        {
            get => inner.Priority;
            set => inner.Priority = value;
        }

        public long? Size
        {
            get => inner.Size;
            set => inner.Size = value;
        }

        public void Dispose()
        {
            onDispose(new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = AbsoluteExpiration,
                AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
                SlidingExpiration = SlidingExpiration,
                Priority = Priority,
                Size = Size
            });
            inner.Dispose();
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
