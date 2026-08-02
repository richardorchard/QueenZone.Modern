using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsAgentQueuedRunProcessorTests
{
    [Fact]
    public async Task RunOnce_completes_claimed_triage_request()
    {
        var repository = CreateRepository();
        await repository.QueueAsync("editor@example.com");
        var executor = new FakeExecutor(new NewsAgentQueuedRunResult(
            0,
            CreateSummary(skipped: false)));
        var processor = new NewsAgentQueuedRunProcessor(
            repository,
            executor,
            NullLogger<NewsAgentQueuedRunProcessor>.Instance);

        var exitCode = await processor.RunOnceAsync("news-pc");
        var request = Assert.Single(await repository.ListRecentAsync());

        Assert.Equal(0, exitCode);
        Assert.Equal(1, executor.Calls);
        Assert.Equal(NewsAgentRunRequestStatus.Completed, request.Status);
        Assert.Contains("Triage run finished", request.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunOnce_returns_request_to_pending_when_lease_is_held()
    {
        var repository = CreateRepository();
        await repository.QueueAsync("editor@example.com");
        var processor = new NewsAgentQueuedRunProcessor(
            repository,
            new FakeExecutor(new NewsAgentQueuedRunResult(0, CreateSummary(skipped: true))),
            NullLogger<NewsAgentQueuedRunProcessor>.Instance);

        var exitCode = await processor.RunOnceAsync("news-pc");
        var request = Assert.Single(await repository.ListRecentAsync());

        Assert.Equal(0, exitCode);
        Assert.Equal(NewsAgentRunRequestStatus.Pending, request.Status);
    }

    private static InMemoryNewsAgentRunRequestRepository CreateRepository() =>
        new(new SharedNewsAgentRunRequestStore());

    private static NewsAgentRunSummary CreateSummary(bool skipped) =>
        new(
            skipped,
            AiEnabled: true,
            DryRun: false,
            Discovery: null,
            Triage: null,
            Draft: null,
            EstimatedAiSpendUsd: 0,
            ExitCode: 0);

    private sealed class FakeExecutor(NewsAgentQueuedRunResult result) : INewsAgentQueuedRunExecutor
    {
        public int Calls { get; private set; }

        public Task<NewsAgentQueuedRunResult> RunAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }
}
