using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfNewsAgentRunRequestRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<QueenZoneDbContext> options;

    public EfNewsAgentRunRequestRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        using var dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task Active_request_is_unique_and_claimed_once_across_contexts()
    {
        await using var firstContext = new QueenZoneDbContext(options);
        var firstRepository = new EfNewsAgentRunRequestRepository(firstContext);
        var first = await firstRepository.QueueAsync(new QueenZone.Data.NewsAgentRunRequestCreate("editor@example.com"));

        await using var secondContext = new QueenZoneDbContext(options);
        var secondRepository = new EfNewsAgentRunRequestRepository(secondContext);
        var duplicate = await secondRepository.QueueAsync(new QueenZone.Data.NewsAgentRunRequestCreate("other@example.com"));
        var claimed = await secondRepository.ClaimNextAsync("news-pc");

        await using var thirdContext = new QueenZoneDbContext(options);
        var thirdRepository = new EfNewsAgentRunRequestRepository(thirdContext);
        var secondClaim = await thirdRepository.ClaimNextAsync("news-pc-2");

        Assert.True(first.WasCreated);
        Assert.False(duplicate.WasCreated);
        Assert.Equal(first.Request.Id, duplicate.Request.Id);
        Assert.Equal(first.Request.Id, claimed?.Id);
        Assert.Null(secondClaim);
    }

    [Fact]
    public async Task Completion_releases_active_slot_and_status_is_persisted()
    {
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfNewsAgentRunRequestRepository(dbContext);
        await repository.QueueAsync(new QueenZone.Data.NewsAgentRunRequestCreate("editor@example.com"));
        var claimed = await repository.ClaimNextAsync("news-pc");

        Assert.True(await repository.CompleteAsync(claimed!.Id, "Completed successfully"));
        var next = await repository.QueueAsync(new QueenZone.Data.NewsAgentRunRequestCreate("editor@example.com"));
        var recent = await repository.ListRecentAsync();

        Assert.True(next.WasCreated);
        Assert.Equal(2, recent.Count);
        Assert.Contains(recent, request =>
            request.Id == claimed.Id && request.Status == NewsAgentRunRequestStatus.Completed);
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
