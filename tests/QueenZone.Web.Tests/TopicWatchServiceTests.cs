using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class TopicWatchServiceTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public TopicWatchServiceTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Watch_Get_Unwatch_OnPublicTopic()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<TopicWatchService>();
        var memberId = Guid.NewGuid();

        var missing = await service.GetStatusAsync(memberId, 9999);
        Assert.Null(missing);
        Assert.Null(await service.WatchAsync(memberId, 9999));
        Assert.Null(await service.UnwatchAsync(memberId, 9999));

        var before = await service.GetStatusAsync(memberId, 1002);
        Assert.NotNull(before);
        Assert.False(before!.Watching);

        var watched = await service.WatchAsync(memberId, 1002);
        Assert.True(watched!.Watching);
        Assert.True((await service.GetStatusAsync(memberId, 1002))!.Watching);
        Assert.True((await service.WatchAsync(memberId, 1002))!.Watching);

        var unwatched = await service.UnwatchAsync(memberId, 1002);
        Assert.False(unwatched!.Watching);
        Assert.False((await service.GetStatusAsync(memberId, 1002))!.Watching);
        Assert.False((await service.UnwatchAsync(memberId, 1002))!.Watching);
    }
}
