using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentRunLeaseServiceTests
{
    [Fact]
    public async Task TryAcquireAsync_allows_only_one_active_holder()
    {
        var service = new InMemoryNewsAgentRunLeaseService(new SharedNewsAgentLeaseStore());
        var duration = TimeSpan.FromMinutes(30);

        var first = await service.TryAcquireAsync("discover-news", duration);
        var second = await service.TryAcquireAsync("discover-news", duration);

        Assert.NotNull(first);
        Assert.Null(second);

        await first!.DisposeAsync();

        var third = await service.TryAcquireAsync("discover-news", duration);
        Assert.NotNull(third);
        await third!.DisposeAsync();
    }

    [Fact]
    public async Task TryAcquireAsync_allows_new_holder_after_release()
    {
        var service = new InMemoryNewsAgentRunLeaseService(new SharedNewsAgentLeaseStore());
        var duration = TimeSpan.FromMinutes(30);

        await using (var lease = (await service.TryAcquireAsync("discover-news", duration))!)
        {
            Assert.NotNull(lease);
        }

        var next = await service.TryAcquireAsync("discover-news", duration);
        Assert.NotNull(next);
        await next!.DisposeAsync();
    }

    [Fact]
    public void Release_returns_false_when_holder_does_not_match()
    {
        var store = new SharedNewsAgentLeaseStore();
        store.TryAcquire("discover-news", "holder-a", DateTime.UtcNow.AddMinutes(30));

        Assert.False(store.Release("discover-news", "holder-b"));
        Assert.True(store.Release("discover-news", "holder-a"));
    }
}
