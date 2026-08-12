using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SearchReindexRunLeaseServiceTests
{
    [Fact]
    public async Task TryAcquireAsync_allows_only_one_active_holder()
    {
        var service = new InMemorySearchReindexRunLeaseService(new SharedSearchReindexLeaseStore());
        var duration = TimeSpan.FromMinutes(30);

        var first = await service.TryAcquireAsync("search-reindex", duration);
        var second = await service.TryAcquireAsync("search-reindex", duration);

        Assert.NotNull(first);
        Assert.Null(second);

        await first!.DisposeAsync();

        var third = await service.TryAcquireAsync("search-reindex", duration);
        Assert.NotNull(third);
        await third!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_allows_new_holder_after_release()
    {
        var service = new InMemorySearchReindexRunLeaseService(new SharedSearchReindexLeaseStore());
        var duration = TimeSpan.FromMinutes(30);

        await using (var lease = (await service.TryAcquireAsync("search-reindex", duration))!)
        {
            Assert.NotNull(lease);
        }

        var next = await service.TryAcquireAsync("search-reindex", duration);
        Assert.NotNull(next);
        await next!.DisposeAsync();
    }

    [Fact]
    public void Release_returns_false_when_holder_does_not_match()
    {
        var store = new SharedSearchReindexLeaseStore();
        store.TryAcquire("search-reindex", "holder-a", DateTime.UtcNow.AddMinutes(30));

        Assert.False(store.Release("search-reindex", "holder-b"));
        Assert.True(store.Release("search-reindex", "holder-a"));
    }
}
