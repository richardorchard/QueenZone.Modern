using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryServiceTests
{
    [Fact]
    public async Task RunFetchAsync_creates_candidates_idempotently_from_rss_fixture()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var feedUrl = "https://www.queenonline.com/feed/";
        var httpClient = new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [feedUrl] = NewsAgentTestSupport.ReadFixture("sample-rss.xml")
        });
        var service = NewsAgentTestSupport.CreateDiscoveryService(repository, httpClient);
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            feedUrl,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

        var firstRun = await service.RunFetchAsync(new NewsDiscoveryRunOptions(Force: true));
        var secondRun = await service.RunFetchAsync(new NewsDiscoveryRunOptions(Force: true));

        Assert.Equal(1, firstRun.SourcesChecked);
        Assert.Equal(2, firstRun.ItemsFetched);
        Assert.Equal(2, firstRun.CandidatesCreated);
        Assert.Equal(0, firstRun.DuplicatesSkipped);
        Assert.Equal(2, secondRun.DuplicatesSkipped);
        Assert.Equal(0, secondRun.CandidatesCreated);

        var candidates = await repository.GetCandidatesAsync(sourceId: sourceId);
        Assert.Equal(2, candidates.Count);
        Assert.Contains(candidates, candidate => candidate.CanonicalUrl == "https://www.queenonline.com/news/tour-2026");
    }

    [Fact]
    public async Task RunFetchAsync_filters_secondary_source_items_without_keyword_match()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var feedUrl = "https://www.nme.com/feed";
        var httpClient = new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [feedUrl] = """
                <?xml version="1.0" encoding="UTF-8"?>
                <rss version="2.0"><channel>
                  <item>
                    <title>Generic guitar pedal review</title>
                    <link>https://www.nme.com/music/pedal-review</link>
                    <description>No band mentioned.</description>
                  </item>
                  <item>
                    <title>Queen documentary announced</title>
                    <link>https://www.nme.com/music/queen-documentary</link>
                    <description>Queen feature.</description>
                  </item>
                </channel></rss>
                """
        });
        var service = NewsAgentTestSupport.CreateDiscoveryService(repository, httpClient);
        await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "nme-music",
            "NME Music",
            "https://www.nme.com/music",
            feedUrl,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Secondary,
            60,
            true,
            "queen,freddie mercury"));

        var result = await service.RunFetchAsync(new NewsDiscoveryRunOptions(Force: true));

        Assert.Equal(2, result.ItemsFetched);
        Assert.Equal(1, result.KeywordFiltered);
        Assert.Equal(1, result.CandidatesCreated);
    }

    [Fact]
    public async Task RunFetchAsync_skips_sources_before_poll_interval_unless_forced()
    {
        var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var feedUrl = "https://www.queenonline.com/feed/";
        var httpClient = new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [feedUrl] = NewsAgentTestSupport.ReadFixture("sample-rss.xml")
        });
        var service = NewsAgentTestSupport.CreateDiscoveryService(repository, httpClient);
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            feedUrl,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        await repository.MarkSourceFetchedAsync(sourceId, DateTime.UtcNow);

        var result = await service.RunFetchAsync(new NewsDiscoveryRunOptions());

        Assert.Equal(0, result.SourcesChecked);
        Assert.Equal(1, result.SourcesSkipped);
    }

    [Fact]
    public static void SeedSources_loads_expected_primary_and_secondary_registry()
    {
        var sources = NewsDiscoverySourceSeeder.LoadSeedSources();

        Assert.True(sources.Count >= 10);
        Assert.Contains(sources, source => source.Key == "queen-online" && source.TrustTier == NewsDiscoveryTrustTier.Primary);
        Assert.Contains(sources, source => source.Key == "nme-music" && source.TrustTier == NewsDiscoveryTrustTier.Secondary);
        Assert.Contains(sources, source => source.Key == "gold-radio" && !source.Enabled);
    }
}
