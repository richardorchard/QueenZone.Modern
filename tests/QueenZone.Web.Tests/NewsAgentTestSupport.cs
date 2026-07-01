using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

internal sealed class FakeNewsDiscoveryHttpClient(IReadOnlyDictionary<string, string> responses) : INewsDiscoveryHttpClient
{
    public Task<string> GetStringAsync(string url, CancellationToken cancellationToken = default)
    {
        if (responses.TryGetValue(url, out var body))
        {
            return Task.FromResult(body);
        }

        throw new HttpRequestException($"No fixture configured for URL '{url}'.");
    }
}

internal static class NewsAgentTestSupport
{
    public static string ReadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "NewsAgent", fileName);
        return File.ReadAllText(path);
    }

    public static NewsDiscoveryService CreateDiscoveryService(
        INewsDiscoveryRepository repository,
        INewsDiscoveryHttpClient httpClient)
    {
        INewsSourceFetcher[] fetchers =
        [
            new RssAtomSourceFetcher(httpClient),
            new SitemapSourceFetcher(httpClient),
            new AllowlistedPageSourceFetcher(httpClient)
        ];

        return new NewsDiscoveryService(
            repository,
            new NewsSourceFetcherRegistry(fetchers),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<NewsDiscoveryService>.Instance);
    }

    public const string SampleDraftJson = """
        {
          "title": "Queen announce 2026 tour",
          "slug": "queen-announce-2026-tour",
          "excerpt": "Queen have announced new 2026 tour dates.",
          "body": "Queen will return to the road in 2026 with dates across Europe and the UK.",
          "related_entities": ["Queen", "tour"],
          "source_urls": ["https://www.queenonline.com/news/tour-2026"],
          "source_names": ["Queen Online"],
          "attribution_text": "Source: Queen Online",
          "confidence_notes": "Primary official source.",
          "source_notes": "Official Queen Online announcement.",
          "suggested_publish_at": "2026-07-02T10:00:00Z",
          "secondary_source_warning": false
        }
        """;

    public static NewsDraftGenerationService CreateDraftGenerationService(
        INewsDiscoveryRepository repository,
        INewsAiClient aiClient)
    {
        var budgetGuard = new NewsAiBudgetGuard(
            repository,
            Microsoft.Extensions.Options.Options.Create(new OpenRouterOptions
            {
                ApiKey = aiClient.IsEnabled ? "test-key" : null,
                PerRunCandidateLimit = 5,
                PerRunBudgetUsd = 1m,
                DailyBudgetUsd = 5m
            }));

        var executor = new NewsAiRunExecutor(
            aiClient,
            repository,
            budgetGuard,
            Microsoft.Extensions.Options.Options.Create(new OpenRouterOptions
            {
                ApiKey = aiClient.IsEnabled ? "test-key" : null
            }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<NewsAiRunExecutor>.Instance);

        return new NewsDraftGenerationService(
            repository,
            executor,
            Microsoft.Extensions.Options.Options.Create(new NewsDraftGenerationOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<NewsDraftGenerationService>.Instance);
    }
}
