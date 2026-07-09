using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentFailureModeTests
{
    [Fact]
    public async Task RunTriageAsync_counts_malformed_ai_response_as_failure()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = NewsAgentTestSupport.CreateTriageService(
            repository,
            new ConfigurableNewsAiClient(enabled: true, content: "{not-json"));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(1, result.Failures);
        Assert.Equal(NewsCandidateStatus.Discovered, (await repository.GetCandidateByIdAsync(candidateId))!.Status);
    }

    [Fact]
    public async Task RunDraftGenerationAsync_counts_malformed_ai_response_as_failure()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
        var service = NewsAgentTestSupport.CreateDraftGenerationService(
            repository,
            new ConfigurableNewsAiClient(enabled: true, content: "{not-json"));

        var result = await service.RunDraftGenerationAsync(new NewsDraftRunOptions());

        Assert.Equal(1, result.Failures);
        Assert.Null(await repository.GetDraftByCandidateIdAsync(candidateId));
    }

    [Fact]
    public async Task RunTriageAsync_without_openrouter_uses_deterministic_checks_only()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = NewsAgentTestSupport.CreateTriageService(
            repository,
            new ConfigurableNewsAiClient(enabled: false, content: "{}"));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(0, result.Failures);
        Assert.True(result.Rejected + result.PromotedToReview + result.MarkedDuplicate + result.Skipped >= 0);
    }

    [Fact]
    public async Task DiscoverNewsWorker_returns_error_exit_when_triage_ai_fails()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var worker = NewsAgentTestSupport.CreateDiscoverNewsWorker(
            repository,
            triageClient: new ConfigurableNewsAiClient(enabled: true, content: "{bad-json"),
            draftClient: new ConfigurableNewsAiClient(enabled: true, content: "{}"),
            executorClient: new ConfigurableNewsAiClient(enabled: true, content: "{}"));

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: false,
            Triage: true,
            TriageOnly: true,
            Draft: false,
            DraftOnly: false));

        Assert.Equal(1, exitCode);
    }
}
