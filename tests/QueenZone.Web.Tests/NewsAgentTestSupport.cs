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
}
