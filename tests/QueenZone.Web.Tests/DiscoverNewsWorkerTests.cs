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

        var worker = CreateWorker(repository, discoveryService, aiEnabled: false);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: true,
            Triage: true,
            TriageOnly: false,
            Draft: false,
            DraftOnly: false));

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

        var worker = CreateWorker(repository, discoveryService, aiEnabled: false);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: true,
            Triage: false,
            TriageOnly: false,
            Draft: false,
            DraftOnly: false));

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task RunAsync_triage_only_skips_discovery_fetch()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        var worker = CreateWorker(repository, discoveryService, aiEnabled: true, dryRun: true);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: false,
            Triage: true,
            TriageOnly: true,
            Draft: false,
            DraftOnly: false));

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_draft_only_generates_drafts_for_needs_review_candidates()
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
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Official dates announced.",
            DateTime.UtcNow));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                ConfidenceScore: 0.90m,
                RelevanceScore: 0.92m));

        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        var worker = CreateWorker(
            repository,
            discoveryService,
            aiEnabled: true,
            draftJson: NewsAgentTestSupport.SampleDraftJson);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: false,
            Triage: false,
            TriageOnly: false,
            Draft: true,
            DraftOnly: true));

        Assert.Equal(0, exitCode);
        Assert.NotNull(await repository.GetDraftByCandidateIdAsync(candidateId));
        Assert.Equal(NewsCandidateStatus.Drafted, (await repository.GetCandidateByIdAsync(candidateId))!.Status);
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

    [Fact]
    public async Task RunAsync_skips_when_run_lease_is_held()
    {
        var leaseStore = new SharedNewsAgentLeaseStore();
        var leaseService = new InMemoryNewsAgentRunLeaseService(leaseStore);
        await using var held = (await leaseService.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var discoveryService = NewsAgentTestSupport.CreateDiscoveryService(
            repository,
            new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        var worker = CreateWorker(
            repository,
            discoveryService,
            aiEnabled: false,
            leaseService: leaseService,
            useRunLease: true);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: false,
            Triage: false,
            TriageOnly: false,
            Draft: false,
            DraftOnly: false));

        Assert.Equal(0, exitCode);
        Assert.Empty(await repository.GetCandidatesAsync());
    }

    [Fact]
    public async Task RunAsync_force_bypasses_run_lease_when_another_holder_is_active()
    {
        var leaseStore = new SharedNewsAgentLeaseStore();
        var leaseService = new InMemoryNewsAgentRunLeaseService(leaseStore);
        await using var held = (await leaseService.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

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

        var worker = CreateWorker(
            repository,
            discoveryService,
            aiEnabled: false,
            leaseService: leaseService,
            useRunLease: true);

        var exitCode = await worker.RunAsync(new DiscoverNewsCommandOptions(
            SeedSources: false,
            DryRun: false,
            Force: true,
            Triage: false,
            TriageOnly: false,
            Draft: false,
            DraftOnly: false));

        Assert.Equal(0, exitCode);
        Assert.Equal(2, (await repository.GetCandidatesAsync()).Count);
    }

    [Fact]
    public void DiscoverNewsCommandOptions_Parse_scheduled_enables_full_pipeline_flags()
    {
        var options = DiscoverNewsCommandOptions.Parse(["discover-news", "--scheduled"]);

        Assert.NotNull(options);
        Assert.True(options.SeedSources);
        Assert.True(options.Triage);
        Assert.True(options.Draft);
        Assert.False(options.TriageOnly);
        Assert.False(options.DraftOnly);
    }

    private static DiscoverNewsWorker CreateWorker(
        INewsDiscoveryRepository repository,
        NewsDiscoveryService discoveryService,
        bool aiEnabled,
        bool dryRun = false,
        string draftJson = "{}",
        INewsAgentRunLeaseService? leaseService = null,
        bool useRunLease = false) =>
        new(
            discoveryService,
            CreateTriageService(repository, aiEnabled),
            NewsAgentTestSupport.CreateDraftGenerationService(
                repository,
                new DiscoverNewsWorkerTestsFakeAiClient(aiEnabled, draftJson)),
            CreateExecutor(repository, aiEnabled),
            repository,
            leaseService ?? new InMemoryNewsAgentRunLeaseService(new SharedNewsAgentLeaseStore()),
            Options.Create(new OpenRouterOptions
            {
                ApiKey = aiEnabled ? "test-key" : null,
                DryRun = dryRun
            }),
            Options.Create(new NewsAgentSchedulerOptions
            {
                UseRunLease = useRunLease,
                LeaseName = "discover-news"
            }),
            NullLogger<DiscoverNewsWorker>.Instance);

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

    private sealed class DiscoverNewsWorkerTestsFakeAiClient(bool enabled, string content = "{}") : INewsAiClient
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
