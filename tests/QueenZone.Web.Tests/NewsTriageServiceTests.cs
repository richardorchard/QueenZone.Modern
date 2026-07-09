using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsTriageServiceTests
{
    [Theory]
    [InlineData(
        """
        {
          "verdict": "relevant",
          "relevance_score": 0.93,
          "confidence_score": 0.90,
          "rationale": "Official Queen tour announcement.",
          "suggested_category": "tour",
          "entities": ["Queen", "tour"],
          "review_notes": "Primary source."
        }
        """,
        NewsCandidateStatus.NeedsReview,
        0.93)]
    [InlineData(
        """
        {
          "verdict": "not_relevant",
          "relevance_score": 0.12,
          "confidence_score": 0.95,
          "rationale": "Generic guitar pedal review.",
          "suggested_category": "other",
          "entities": [],
          "review_notes": null
        }
        """,
        NewsCandidateStatus.Rejected,
        null)]
    public async Task TriageCandidateAsync_maps_ai_verdict_to_expected_status(
        string aiResponseJson,
        NewsCandidateStatus expectedStatus,
        double? expectedRelevanceScore)
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = CreateService(repository, new FakeNewsAiClient(true, aiResponseJson));

        var result = await service.TriageCandidateAsync(
            (await repository.GetCandidateByIdAsync(candidateId))!,
            new NewsTriageRunOptions());

        Assert.Equal(expectedStatus, result.Decision.TargetStatus);
        var updated = await repository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(updated);
        Assert.Equal(expectedStatus, updated.Status);
        if (expectedRelevanceScore is not null)
        {
            Assert.Equal((decimal)expectedRelevanceScore.Value, updated.RelevanceScore);
        }
    }

    [Fact]
    public async Task TriageCandidateAsync_rejects_candidates_outside_discovered_status()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
        var candidate = (await repository.GetCandidateByIdAsync(candidateId))!;
        var service = CreateService(repository, new FakeNewsAiClient(true, "{}"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.TriageCandidateAsync(candidate, new NewsTriageRunOptions()));

        Assert.Contains("discovered", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TriageCandidateAsync_marks_title_duplicate_without_calling_ai()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
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
        var firstId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Official dates announced.",
            DateTime.UtcNow));
        await repository.TryUpdateCandidateStatusAsync(
            firstId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        var duplicateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026?utm=test",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Different excerpt.",
            DateTime.UtcNow));

        var fakeClient = new FakeNewsAiClient(true, shouldNotBeCalled: true);
        var service = CreateService(repository, fakeClient);
        var result = await service.TriageCandidateAsync(
            (await repository.GetCandidateByIdAsync(duplicateId))!,
            new NewsTriageRunOptions());

        Assert.Equal(NewsCandidateStatus.IgnoredDuplicate, result.Decision.TargetStatus);
        Assert.False(result.Decision.UsedAi);
        Assert.False(fakeClient.WasCalled);
        var updated = await repository.GetCandidateByIdAsync(duplicateId);
        Assert.Equal(firstId, updated!.DuplicateOfCandidateId);
    }

    [Fact]
    public async Task RunTriageAsync_skips_ai_when_openrouter_disabled()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = CreateService(repository, new FakeNewsAiClient(false));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(1, result.CandidatesConsidered);
        Assert.Equal(0, result.PromotedToReview);
        Assert.Equal(0, result.Failures);
    }

    [Fact]
    public async Task RunTriageAsync_returns_zero_counts_when_no_candidates()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var service = CreateService(repository, new FakeNewsAiClient(true));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(0, result.CandidatesConsidered);
    }

    [Fact]
    public async Task TriageCandidateAsync_dry_run_does_not_persist_status_changes()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = CreateService(
            repository,
            new FakeNewsAiClient(
                true,
                """
                {
                  "verdict": "relevant",
                  "relevance_score": 0.93,
                  "confidence_score": 0.90,
                  "rationale": "Official Queen tour announcement.",
                  "suggested_category": "tour",
                  "entities": ["Queen"],
                  "review_notes": null
                }
                """));

        await service.TriageCandidateAsync(
            (await repository.GetCandidateByIdAsync(candidateId))!,
            new NewsTriageRunOptions(DryRun: true));

        var candidate = await repository.GetCandidateByIdAsync(candidateId);
        Assert.Equal(NewsCandidateStatus.Discovered, candidate!.Status);
    }

    [Fact]
    public async Task RunTriageAsync_records_failure_when_ai_returns_invalid_json()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        await NewsDiscoveryTestSeeder.SeedDiscoveredCandidateAsync(repository);
        var service = CreateService(repository, new FakeNewsAiClient(true, "not-json"));

        var result = await service.RunTriageAsync(new NewsTriageRunOptions());

        Assert.Equal(1, result.Failures);
        Assert.Single(result.Errors);
    }

    private static NewsTriageService CreateService(INewsDiscoveryRepository repository, FakeNewsAiClient fakeClient)
    {
        var budgetGuard = new NewsAiBudgetGuard(
            repository,
            Options.Create(new OpenRouterOptions
            {
                ApiKey = fakeClient.IsEnabled ? "test-key" : null,
                PerRunCandidateLimit = 5,
                PerRunBudgetUsd = 1m,
                DailyBudgetUsd = 5m
            }));
        var executor = new NewsAiRunExecutor(
            fakeClient,
            repository,
            budgetGuard,
            Options.Create(new OpenRouterOptions
            {
                ApiKey = fakeClient.IsEnabled ? "test-key" : null
            }),
            NullLogger<NewsAiRunExecutor>.Instance);

        return new NewsTriageService(
            repository,
            executor,
            new NewsTriageDeterministicAnalyzer(repository),
            Options.Create(new NewsTriageOptions()),
            NullLogger<NewsTriageService>.Instance);
    }

    private sealed class FakeNewsAiClient(bool enabled, string? content = null, bool shouldNotBeCalled = false) : INewsAiClient
    {
        public bool WasCalled { get; private set; }

        public bool IsEnabled { get; } = enabled;

        public Task<NewsAiChatCompletion> CompleteChatAsync(
            NewsAiChatRequest request,
            CancellationToken cancellationToken = default)
        {
            if (shouldNotBeCalled)
            {
                throw new InvalidOperationException("AI client should not have been called.");
            }

            WasCalled = true;
            return Task.FromResult(new NewsAiChatCompletion(
                content ?? string.Empty,
                "openai/gpt-4.1-nano",
                100,
                20,
                0.0001m,
                DryRun: false));
        }
    }
}
