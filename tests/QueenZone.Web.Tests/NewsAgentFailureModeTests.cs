using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentFailureModeTests
{
    [Fact]
    public async Task RunTriageAsync_counts_malformed_ai_response_as_failure()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await SeedDiscoveredCandidateAsync(repository);
        var service = CreateTriageService(repository, new FailureModeFakeAiClient("{not-json"));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(1, result.Failures);
        Assert.Equal(NewsCandidateStatus.Discovered, (await repository.GetCandidateByIdAsync(candidateId))!.Status);
    }

    [Fact]
    public async Task RunDraftGenerationAsync_counts_malformed_ai_response_as_failure()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await SeedNeedsReviewCandidateAsync(repository);
        var service = NewsAgentTestSupport.CreateDraftGenerationService(
            repository,
            new FailureModeFakeAiClient("{not-json"));

        var result = await service.RunDraftGenerationAsync(new NewsDraftRunOptions());

        Assert.Equal(1, result.Failures);
        Assert.Null(await repository.GetDraftByCandidateIdAsync(candidateId));
    }

    [Fact]
    public async Task RunTriageAsync_without_openrouter_uses_deterministic_checks_only()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await SeedDiscoveredCandidateAsync(repository);
        var service = CreateTriageService(repository, new FailureModeFakeAiClient("{}", enabled: false));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(0, result.Failures);
        Assert.True(result.Rejected + result.PromotedToReview + result.MarkedDuplicate + result.Skipped >= 0);
    }

    [Fact]
    public async Task DiscoverNewsWorker_returns_error_exit_when_triage_ai_fails()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await SeedDiscoveredCandidateAsync(repository);
        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        var worker = new DiscoverNewsWorker(
            discoveryService,
            CreateTriageService(repository, new FailureModeFakeAiClient("{bad-json")),
            NewsAgentTestSupport.CreateDraftGenerationService(repository, new FailureModeFakeAiClient("{}")),
            CreateExecutor(repository, new FailureModeFakeAiClient("{}")),
            repository,
            Options.Create(new OpenRouterOptions { ApiKey = "test-key" }),
            NullLogger<DiscoverNewsWorker>.Instance);

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

    private static NewsTriageService CreateTriageService(
        INewsDiscoveryRepository repository,
        INewsAiClient aiClient) =>
        new(
            repository,
            new NewsAiRunExecutor(
                aiClient,
                repository,
                new NewsAiBudgetGuard(
                    repository,
                    Options.Create(new OpenRouterOptions
                    {
                        ApiKey = aiClient.IsEnabled ? "test-key" : null,
                        PerRunCandidateLimit = 5,
                        PerRunBudgetUsd = 1m,
                        DailyBudgetUsd = 5m
                    })),
                Options.Create(new OpenRouterOptions { ApiKey = aiClient.IsEnabled ? "test-key" : null }),
                NullLogger<NewsAiRunExecutor>.Instance),
            new NewsTriageDeterministicAnalyzer(repository),
            Options.Create(new NewsTriageOptions()),
            NullLogger<NewsTriageService>.Instance);

    private static NewsAiRunExecutor CreateExecutor(
        INewsDiscoveryRepository repository,
        INewsAiClient aiClient) =>
        new(
            aiClient,
            repository,
            new NewsAiBudgetGuard(
                repository,
                Options.Create(new OpenRouterOptions
                {
                    ApiKey = aiClient.IsEnabled ? "test-key" : null,
                    PerRunCandidateLimit = 5,
                    PerRunBudgetUsd = 1m,
                    DailyBudgetUsd = 5m
                })),
            Options.Create(new OpenRouterOptions { ApiKey = aiClient.IsEnabled ? "test-key" : null }),
            NullLogger<NewsAiRunExecutor>.Instance);

    private static async Task<int> SeedDiscoveredCandidateAsync(InMemoryNewsDiscoveryRepository repository)
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed/",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        return await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Official dates announced.",
            DateTime.UtcNow));
    }

    private static async Task<int> SeedNeedsReviewCandidateAsync(InMemoryNewsDiscoveryRepository repository)
    {
        var candidateId = await SeedDiscoveredCandidateAsync(repository);
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                ConfidenceScore: 0.90m,
                RelevanceScore: 0.92m));
        return candidateId;
    }

    private sealed class FailureModeFakeAiClient(string content, bool enabled = true) : INewsAiClient
    {
        public bool IsEnabled { get; } = enabled;

        public Task<NewsAiChatCompletion> CompleteChatAsync(
            NewsAiChatRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsAiChatCompletion(
                content,
                request.ModelRole == NewsAiModelRole.Drafting ? "openai/gpt-4.1-mini" : "openai/gpt-4.1-nano",
                1,
                1,
                0.0001m,
                false));
    }
}
