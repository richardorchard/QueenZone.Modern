using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAiBudgetGuardTests
{
    [Fact]
    public async Task EnsureWithinBudgetAsync_allows_processing_under_limits()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var guard = CreateGuard(repository, perRunBudget: 1m, dailyBudget: 2m, candidateLimit: 5);

        guard.BeginRun();
        guard.RegisterCandidateAttempt();

        var exception = await Record.ExceptionAsync(() => guard.EnsureWithinBudgetAsync(DateTime.UtcNow));

        Assert.Null(exception);
    }

    [Fact]
    public void RegisterCandidateAttempt_throws_when_per_run_candidate_limit_exceeded()
    {
        var guard = CreateGuard(new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore()), candidateLimit: 1);
        guard.BeginRun();
        guard.RegisterCandidateAttempt();

        Assert.Throws<NewsAiBudgetExceededException>(() => guard.RegisterCandidateAttempt());
    }

    [Fact]
    public async Task EnsureWithinBudgetAsync_throws_when_daily_budget_already_spent()
    {
        var store = new SharedNewsDiscoveryStore();
        var repository = new InMemoryNewsDiscoveryRepository(store);
        var utcNow = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        await SeedCompletedAiRunAsync(repository, store, utcNow, 2.00m);

        var guard = CreateGuard(repository, dailyBudget: 2m);
        guard.BeginRun();

        await Assert.ThrowsAsync<NewsAiBudgetExceededException>(() => guard.EnsureWithinBudgetAsync(utcNow));
    }

    [Fact]
    public void RegisterSpend_throws_when_per_run_budget_exceeded()
    {
        var guard = CreateGuard(new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore()), perRunBudget: 0.10m);
        guard.BeginRun();

        guard.RegisterSpend(0.08m);

        Assert.Throws<NewsAiBudgetExceededException>(() => guard.RegisterSpend(0.05m));
    }

    private static NewsAiBudgetGuard CreateGuard(
        INewsDiscoveryRepository repository,
        decimal perRunBudget = 0.50m,
        decimal dailyBudget = 2m,
        int candidateLimit = 25) =>
        new(
            repository,
            Options.Create(new OpenRouterOptions
            {
                PerRunBudgetUsd = perRunBudget,
                DailyBudgetUsd = dailyBudget,
                PerRunCandidateLimit = candidateLimit
            }));

    private static async Task SeedCompletedAiRunAsync(
        InMemoryNewsDiscoveryRepository repository,
        SharedNewsDiscoveryStore store,
        DateTime completedAt,
        decimal estimatedCostUsd)
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
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/test",
            "Queen test headline",
            completedAt,
            "Excerpt",
            completedAt));
        var aiRunId = await repository.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "openai/gpt-4.1-nano",
            "triage-v1",
            completedAt));
        await repository.CompleteAiRunAsync(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded,
            100,
            50,
            estimatedCostUsd,
            """{"relevant":true}""",
            null,
            completedAt));
    }
}
