using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfSearchReindexRunRequestRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<QueenZoneDbContext> options;

    public EfSearchReindexRunRequestRepositoryTests()
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
        var firstRepository = new EfSearchReindexRunRequestRepository(firstContext);
        var first = await firstRepository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));

        await using var secondContext = new QueenZoneDbContext(options);
        var secondRepository = new EfSearchReindexRunRequestRepository(secondContext);
        var duplicate = await secondRepository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await secondRepository.ClaimNextAsync("worker-pc");

        await using var thirdContext = new QueenZoneDbContext(options);
        var thirdRepository = new EfSearchReindexRunRequestRepository(thirdContext);
        var secondClaim = await thirdRepository.ClaimNextAsync("worker-pc-2");

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
        var repository = new EfSearchReindexRunRequestRepository(dbContext);
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await repository.ClaimNextAsync("worker-pc");

        Assert.True(await repository.CompleteAsync(claimed!.Id, "Completed successfully"));
        var next = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var recent = await repository.ListRecentAsync();

        Assert.True(next.WasCreated);
        Assert.Equal(2, recent.Count);
        Assert.Contains(recent, request =>
            request.Id == claimed.Id && request.Status == SearchReindexRunRequestStatus.Completed);
    }

    [Fact]
    public async Task Fail_releases_active_slot_and_records_error_message()
    {
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfSearchReindexRunRequestRepository(dbContext);
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await repository.ClaimNextAsync("worker-pc");

        Assert.True(await repository.FailAsync(claimed!.Id, "boom"));
        var next = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var recent = await repository.ListRecentAsync();

        Assert.True(next.WasCreated);
        Assert.Contains(recent, request =>
            request.Id == claimed.Id
            && request.Status == SearchReindexRunRequestStatus.Failed
            && request.ErrorMessage == "boom");
    }

    [Fact]
    public async Task ReturnToPending_allows_a_later_claim_to_retry()
    {
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfSearchReindexRunRequestRepository(dbContext);
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await repository.ClaimNextAsync("worker-pc");

        Assert.True(await repository.ReturnToPendingAsync(claimed!.Id));

        var retried = await repository.ClaimNextAsync("worker-pc");
        Assert.Equal(claimed.Id, retried?.Id);
    }

    [Fact]
    public async Task ClaimNextAsync_reclaims_stale_running_requests()
    {
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfSearchReindexRunRequestRepository(dbContext);
        var queued = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));

        var staleUpdatedAt = DateTime.UtcNow.AddHours(-4);
        await dbContext.SearchReindexRunRequests
            .Where(request => request.Id == queued.Request.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(request => request.Status, SearchReindexRunRequestStatus.Running)
                .SetProperty(request => request.RunnerId, "crashed-worker")
                .SetProperty(request => request.StartedAtUtc, staleUpdatedAt)
                .SetProperty(request => request.UpdatedAtUtc, staleUpdatedAt));

        var reclaimed = await repository.ClaimNextAsync("worker-pc-2");

        Assert.NotNull(reclaimed);
        Assert.Equal(queued.Request.Id, reclaimed!.Id);
        Assert.Equal("worker-pc-2", reclaimed.RunnerId);
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
