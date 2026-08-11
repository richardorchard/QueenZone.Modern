using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web.Search;

namespace QueenZone.Web.Tests;

public sealed class SearchReindexScheduledWorkerTests
{
    [Fact]
    public async Task RunAsync_reindexes_and_completes_the_request()
    {
        var leaseStore = new SharedSearchReindexLeaseStore();
        var requestStore = new SharedSearchReindexRunRequestStore();
        var worker = CreateWorker(leaseStore, requestStore);

        var exitCode = await worker.RunAsync(new SearchReindexCommandOptions(Force: false));

        Assert.Equal(0, exitCode);
        Assert.NotNull(worker.LastRunSummary);
        Assert.False(worker.LastRunSummary!.SkippedDueToLease);
        Assert.False(worker.LastRunSummary!.SkippedNoClaim);

        var requestRepository = new InMemorySearchReindexRunRequestRepository(requestStore);
        var recent = await requestRepository.ListRecentAsync();
        Assert.Single(recent);
        Assert.Equal(SearchReindexRunRequestStatus.Completed, recent[0].Status);
    }

    [Fact]
    public async Task RunAsync_skips_when_run_lease_is_held()
    {
        var leaseStore = new SharedSearchReindexLeaseStore();
        var requestStore = new SharedSearchReindexRunRequestStore();
        var leaseService = new InMemorySearchReindexRunLeaseService(leaseStore);
        await using var held = (await leaseService.TryAcquireAsync("search-reindex", TimeSpan.FromMinutes(30)))!;

        var worker = CreateWorker(leaseStore, requestStore);

        var exitCode = await worker.RunAsync(new SearchReindexCommandOptions(Force: false));

        Assert.Equal(0, exitCode);
        Assert.True(worker.LastRunSummary!.SkippedDueToLease);

        var requestRepository = new InMemorySearchReindexRunRequestRepository(requestStore);
        Assert.Empty(await requestRepository.ListRecentAsync());
    }

    [Fact]
    public async Task RunAsync_force_bypasses_run_lease_when_another_holder_is_active()
    {
        var leaseStore = new SharedSearchReindexLeaseStore();
        var requestStore = new SharedSearchReindexRunRequestStore();
        var leaseService = new InMemorySearchReindexRunLeaseService(leaseStore);
        await using var held = (await leaseService.TryAcquireAsync("search-reindex", TimeSpan.FromMinutes(30)))!;

        var worker = CreateWorker(leaseStore, requestStore);

        var exitCode = await worker.RunAsync(new SearchReindexCommandOptions(Force: true));

        Assert.Equal(0, exitCode);
        Assert.False(worker.LastRunSummary!.SkippedDueToLease);
    }

    [Fact]
    public void SearchReindexCommandOptions_Parse_requires_reindex_verb()
    {
        Assert.Null(SearchReindexCommandOptions.Parse([]));
        Assert.Null(SearchReindexCommandOptions.Parse(["bogus"]));
        Assert.Null(SearchReindexCommandOptions.Parse(["reindex", "--unknown"]));
    }

    [Fact]
    public void SearchReindexCommandOptions_Parse_reads_force_flag()
    {
        var scheduled = SearchReindexCommandOptions.Parse(["reindex", "--scheduled"]);
        var forced = SearchReindexCommandOptions.Parse(["reindex", "--force"]);

        Assert.NotNull(scheduled);
        Assert.False(scheduled.Force);
        Assert.NotNull(forced);
        Assert.True(forced.Force);
    }

    private static SearchReindexScheduledWorker CreateWorker(
        SharedSearchReindexLeaseStore leaseStore,
        SharedSearchReindexRunRequestStore requestStore)
    {
        var searchIndexStore = new SharedSearchIndexStore();
        var searchReindexBuilder = new SearchReindexBuilder(
            new InMemorySearchIndexService(searchIndexStore),
            new InMemoryNewsRepository(new SharedNewsStore(SampleNewsData.CreateSeedArticles())),
            new InMemoryForumRepository(
                SampleForumData.CreateSeedCategories(),
                SampleForumData.CreateSeedStats(),
                new InMemoryForumWriteRepository(),
                new InMemoryForumAttachmentRepository()),
            new InMemoryArticleRepository(new InMemoryArticleSubmissionRepository()),
            new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()),
            new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters()),
            new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums()),
            new InMemoryQueenHistoryRepository(SampleQueenHistoryData.CreateSeedEvents()),
            new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances()));

        return new SearchReindexScheduledWorker(
            searchReindexBuilder,
            new InMemorySearchReindexRunLeaseService(leaseStore),
            new InMemorySearchReindexRunRequestRepository(requestStore),
            Options.Create(new SearchReindexSchedulerOptions
            {
                UseRunLease = true,
                LeaseName = "search-reindex",
                LeaseDurationMinutes = 30,
            }),
            NullLogger<SearchReindexScheduledWorker>.Instance);
    }
}
