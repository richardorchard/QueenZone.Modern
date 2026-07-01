using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class DiscoverNewsWorkerTests
{
    [Fact]
    public async Task RunAsync_runs_fetch_and_triage_when_requested()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var feedUrl = "https://www.queenonline.com/feed/";
        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
            {
                [feedUrl] = NewsAgentTestSupport.ReadFixture("sample-rss.xml")
            }));
        await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            feedUrl,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

        var triageService = CreateTriageService(repository, enabled: false);
        var worker = new DiscoverNewsWorker(
            discoveryService,
            triageService,
            CreateExecutor(repository, enabled: false),
            Options.Create(new OpenRouterOptions()),
            NullLogger<DiscoverNewsWorker>.Instance);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: true,
            Triage: true,
            TriageOnly: false));

        Assert.Equal(0, exitCode);
        Assert.Equal(2, (await repository.GetCandidatesAsync()).Count);
    }

    [Fact]
    public async Task RunAsync_returns_error_code_when_discovery_fails()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var feedUrl = "https://www.queenonline.com/feed/";
        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            feedUrl,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

        var worker = new DiscoverNewsWorker(
            discoveryService,
            CreateTriageService(repository, enabled: false),
            CreateExecutor(repository, enabled: false),
            Options.Create(new OpenRouterOptions()),
            NullLogger<DiscoverNewsWorker>.Instance);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: true,
            Triage: false,
            TriageOnly: false));

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task RunAsync_triage_only_skips_discovery_fetch()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var worker = new DiscoverNewsWorker(
            NewsAgentTestSupport.CreateDiscoveryService(
                repository,
                new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>())),
            CreateTriageService(repository, enabled: false),
            CreateExecutor(repository, enabled: false),
            Options.Create(new OpenRouterOptions { DryRun = true, ApiKey = "test-key" }),
            NullLogger<DiscoverNewsWorker>.Instance);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: false,
            Triage: true,
            TriageOnly: true));

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void AddQueenZoneNewsAgent_binds_news_triage_options_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NewsTriage:SecondaryMinRelevanceScore"] = "0.80"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgent(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NewsTriageOptions>>().Value;

        Assert.Equal(0.80m, options.SecondaryMinRelevanceScore);
    }

    private static NewsTriageService CreateTriageService(INewsDiscoveryRepository repository, bool enabled) =>
        new(
            repository,
            CreateExecutor(repository, enabled),
            new NewsTriageDeterministicAnalyzer(repository),
            Options.Create(new NewsTriageOptions()),
            NullLogger<NewsTriageService>.Instance);

    private static NewsAiRunExecutor CreateExecutor(INewsDiscoveryRepository repository, bool enabled) =>
        new(
            new DiscoverNewsWorkerTestsFakeAiClient(enabled),
            repository,
            new NewsAiBudgetGuard(
                repository,
                Options.Create(new OpenRouterOptions
                {
                    ApiKey = enabled ? "test-key" : null,
                    PerRunCandidateLimit = 5,
                    PerRunBudgetUsd = 1m,
                    DailyBudgetUsd = 5m
                })),
            Options.Create(new OpenRouterOptions { ApiKey = enabled ? "test-key" : null }),
            NullLogger<NewsAiRunExecutor>.Instance);

    private sealed class DiscoverNewsWorkerTestsFakeAiClient(bool enabled) : INewsAiClient
    {
        public bool IsEnabled { get; } = enabled;

        public Task<NewsAiChatCompletion> CompleteChatAsync(
            NewsAiChatRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new NewsAiChatCompletion("{}", "openai/gpt-4.1-nano", 1, 1, 0.0001m, false));
    }
}
