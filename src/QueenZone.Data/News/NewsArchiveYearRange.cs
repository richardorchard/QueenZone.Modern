namespace QueenZone.Data;

/// <summary>
/// Earliest/latest published years in the news archive, in UTC. Both are <see langword="null"/>
/// when there are no published articles. Backs the mobile year-rail scrubber (issue #886), which
/// needs the bounds to lay out its tick marks without walking every article.
/// </summary>
public readonly record struct NewsArchiveYearRange(int? MinYear, int? MaxYear);

/// <summary>In-memory equivalent of the SQL <c>MIN/MAX(PublishedAt)</c> query, shared by the sample/in-memory repositories.</summary>
internal static class NewsArchiveYearRanges
{
    public static NewsArchiveYearRange Compute(IReadOnlyList<NewsItem> items)
    {
        if (items.Count == 0)
        {
            return new NewsArchiveYearRange(null, null);
        }

        var minYear = items.Min(item => item.PublishedAt.Year);
        var maxYear = items.Max(item => item.PublishedAt.Year);
        return new NewsArchiveYearRange(minYear, maxYear);
    }
}
