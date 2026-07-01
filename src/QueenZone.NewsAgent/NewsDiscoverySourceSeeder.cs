using System.Reflection;
using System.Text.Json;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDiscoverySourceSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<NewsDiscoverySourceDraft> LoadSeedSources()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QueenZone.NewsAgent.news-discovery-sources.json")
            ?? throw new InvalidOperationException("Embedded news discovery seed file was not found.");

        var records = JsonSerializer.Deserialize<List<SeedRecord>>(stream, JsonOptions)
            ?? throw new InvalidOperationException("News discovery seed file could not be parsed.");

        return records.Select(record => record.ToDraft()).ToList();
    }

    public static async Task<int> SeedAsync(INewsDiscoveryRepository repository, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var source in LoadSeedSources())
        {
            await repository.UpsertSourceAsync(source, cancellationToken);
            count++;
        }

        return count;
    }

    private sealed record SeedRecord(
        string Key,
        string DisplayName,
        string HomepageUrl,
        string? FeedOrSiteUrl,
        string SourceType,
        string TrustTier,
        int PollIntervalMinutes,
        bool Enabled,
        string? RelevanceKeywords)
    {
        public NewsDiscoverySourceDraft ToDraft() =>
            new(
                Key,
                DisplayName,
                HomepageUrl,
                FeedOrSiteUrl,
                Enum.Parse<NewsDiscoverySourceType>(SourceType, ignoreCase: true),
                Enum.Parse<NewsDiscoveryTrustTier>(TrustTier, ignoreCase: true),
                PollIntervalMinutes,
                Enabled,
                RelevanceKeywords);
    }
}
