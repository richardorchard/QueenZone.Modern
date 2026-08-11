using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SearchReindexRunRequestRepositoryTests
{
    [Fact]
    public async Task Queue_deduplicates_active_request_and_allows_next_after_completion()
    {
        var repository = CreateRepository();

        var first = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var duplicate = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));

        Assert.True(first.WasCreated);
        Assert.False(duplicate.WasCreated);
        Assert.Equal(first.Request.Id, duplicate.Request.Id);

        var claimed = await repository.ClaimNextAsync("worker-pc");
        Assert.NotNull(claimed);
        Assert.Equal(SearchReindexRunRequestStatus.Running, claimed.Status);
        Assert.True(await repository.CompleteAsync(claimed.Id, "Done"));

        var next = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        Assert.True(next.WasCreated);
        Assert.NotEqual(first.Request.Id, next.Request.Id);
    }

    [Fact]
    public async Task Claim_is_single_consumer()
    {
        var repository = CreateRepository();
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));

        var first = await repository.ClaimNextAsync("worker-pc");
        var second = await repository.ClaimNextAsync("worker-pc-2");

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task Return_to_pending_allows_a_later_poll_to_retry()
    {
        var repository = CreateRepository();
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await repository.ClaimNextAsync("worker-pc");

        Assert.True(await repository.ReturnToPendingAsync(claimed!.Id));

        var retried = await repository.ClaimNextAsync("worker-pc");
        Assert.Equal(claimed.Id, retried?.Id);
    }

    [Fact]
    public async Task Fail_records_error_message_and_clears_active_slot()
    {
        var repository = CreateRepository();
        await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        var claimed = await repository.ClaimNextAsync("worker-pc");

        Assert.True(await repository.FailAsync(claimed!.Id, "boom"));

        var next = await repository.QueueAsync(new SearchReindexRunRequestCreate("worker-pc"));
        Assert.True(next.WasCreated);
        Assert.NotEqual(claimed.Id, next.Request.Id);
    }

    private static InMemorySearchReindexRunRequestRepository CreateRepository() =>
        new(new SharedSearchReindexRunRequestStore());
}
