using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsAgentUrlIngestionServiceTests
{
    private const string ArticleUrl = "https://www.queenonline.com/news/manual-submit";

    [Fact]
    public async Task Ingest_creates_candidate_and_triages_without_draft_by_default()
    {
        var store = new SharedNewsDiscoveryStore();
        var repository = new InMemoryNewsDiscoveryRepository(store);
        var triageJson = """
            {
              "verdict": "relevant",
              "rationale": "Official Queen news.",
              "relevance_score": 0.95,
              "confidence_score": 0.9,
              "entities": ["Queen"],
              "review_notes": "Good candidate"
            }
            """;
        var service = CreateService(
            repository,
            new Dictionary<string, string>
            {
                [ArticleUrl] = """
                    <html><head>
                      <title>Queen announce 2026 tour</title>
                      <meta name="description" content="Queen have announced new 2026 tour dates." />
                    </head><body>Brian May said "We love the fans".</body></html>
                    """
            },
            triageJson: triageJson,
            draftJson: NewsAgentTestSupport.SampleDraftJson);

        var result = await service.IngestAsync(ArticleUrl, generateDraft: false);

        Assert.Equal(0, result.ExitCode);
        Assert.False(result.DraftGenerated);
        Assert.NotNull(result.CandidateId);
        var candidate = await repository.GetCandidateByIdAsync(result.CandidateId!.Value);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.NeedsReview, candidate.Status);
        Assert.Null(await repository.GetDraftByCandidateIdAsync(candidate.Id));
        Assert.Contains("triage-only", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ingest_with_generate_draft_creates_draft_but_does_not_publish()
    {
        var store = new SharedNewsDiscoveryStore();
        var repository = new InMemoryNewsDiscoveryRepository(store);
        var triageJson = """
            {
              "verdict": "relevant",
              "rationale": "Official Queen news.",
              "relevance_score": 0.95,
              "confidence_score": 0.9,
              "entities": ["Queen"],
              "review_notes": "Good candidate"
            }
            """;
        var service = CreateService(
            repository,
            new Dictionary<string, string>
            {
                [ArticleUrl] = """
                    <html><head>
                      <title>Queen announce 2026 tour</title>
                      <meta name="description" content="Queen have announced new 2026 tour dates." />
                    </head><body>Story body</body></html>
                    """
            },
            triageJson: triageJson,
            draftJson: NewsAgentTestSupport.SampleDraftJson);

        var result = await service.IngestAsync(ArticleUrl, generateDraft: true);

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.DraftGenerated);
        var candidate = await repository.GetCandidateByIdAsync(result.CandidateId!.Value);
        Assert.Equal(NewsCandidateStatus.Drafted, candidate!.Status);
        Assert.NotNull(await repository.GetDraftByCandidateIdAsync(candidate.Id));
        Assert.Null(candidate.PromotedNewsId);
    }

    [Fact]
    public async Task Ingest_reuses_duplicate_canonical_url()
    {
        var store = new SharedNewsDiscoveryStore();
        var repository = new InMemoryNewsDiscoveryRepository(store);
        var service = CreateService(
            repository,
            new Dictionary<string, string>
            {
                [ArticleUrl] = """
                    <html><head><title>Queen announce 2026 tour</title>
                    <meta name="description" content="Queen have announced new 2026 tour dates." /></head></html>
                    """
            },
            triageJson: """
                {
                  "verdict": "relevant",
                  "rationale": "Official Queen news.",
                  "relevance_score": 0.95,
                  "confidence_score": 0.9,
                  "entities": ["Queen"],
                  "review_notes": "Good candidate"
                }
                """);

        var first = await service.IngestAsync(ArticleUrl, generateDraft: false);
        var second = await service.IngestAsync(ArticleUrl, generateDraft: false);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.True(second.WasDuplicate);
        Assert.Equal(first.CandidateId, second.CandidateId);
        var all = await repository.GetCandidatesAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task Ingest_rejects_unsafe_url_without_fetch()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var service = CreateService(repository, new Dictionary<string, string>());

        var result = await service.IngestAsync("http://127.0.0.1/secret", generateDraft: false);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("blocked", result.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await repository.GetCandidatesAsync());
    }

    private static NewsAgentUrlIngestionService CreateService(
        INewsDiscoveryRepository repository,
        IReadOnlyDictionary<string, string> responses,
        string triageJson = "{}",
        string draftJson = "{}")
    {
        var triageClient = new ConfigurableNewsAiClient(enabled: true, triageJson);
        var draftClient = new ConfigurableNewsAiClient(enabled: true, draftJson);
        return new NewsAgentUrlIngestionService(
            repository,
            new FakeNewsDiscoveryHttpClient(responses),
            NewsAgentTestSupport.CreateTriageService(repository, triageClient),
            NewsAgentTestSupport.CreateDraftGenerationService(repository, draftClient),
            NullLogger<NewsAgentUrlIngestionService>.Instance);
    }
}
