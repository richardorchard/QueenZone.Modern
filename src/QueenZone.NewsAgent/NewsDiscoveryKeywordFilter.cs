using QueenZone.Data;

namespace QueenZone.NewsAgent;

public static class NewsDiscoveryKeywordFilter
{
    public static bool Matches(NewsDiscoverySource source, FetchedNewsItem item)
    {
        if (source.TrustTier == NewsDiscoveryTrustTier.Primary)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(source.RelevanceKeywords))
        {
            return true;
        }

        var haystack = $"{item.Title} {item.Excerpt}".ToLowerInvariant();
        return source.RelevanceKeywords
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(keyword => haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
