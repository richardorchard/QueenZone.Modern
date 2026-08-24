namespace QueenZone.Data;

/// <summary>
/// Optional decade filter for <see cref="INewsRepository.GetArchivePageAsync"/> and
/// <see cref="INewsRepository.GetPublishedCountAsync"/>.
/// </summary>
/// <remarks>
/// News runs roughly 2006-present, unlike the older biography/discography timelines. Filtering
/// client-side over whatever page happens to be loaded produces false "no articles" results for
/// older decades (issue #838), so the decade bound is pushed down to the repository/SQL layer
/// instead.
/// </remarks>
public readonly record struct NewsArchiveFilter(int? DecadeStartYear)
{
    public static readonly NewsArchiveFilter None = default;

    public bool IsActive => DecadeStartYear is not null;

    /// <summary>Inclusive start / exclusive end of the decade, in UTC.</summary>
    public (DateTime Start, DateTime End) GetDecadeBounds()
    {
        if (DecadeStartYear is not { } start)
        {
            throw new InvalidOperationException($"{nameof(GetDecadeBounds)} requires an active filter.");
        }

        return (
            new DateTime(start, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(start + 10, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}

/// <summary>In-memory equivalent of the SQL decade WHERE clause, shared by the sample/in-memory repositories.</summary>
internal static class NewsArchiveFiltering
{
    public static IReadOnlyList<NewsItem> Apply(IReadOnlyList<NewsItem> items, NewsArchiveFilter filter)
    {
        if (!filter.IsActive)
        {
            return items;
        }

        var (start, end) = filter.GetDecadeBounds();
        return items.Where(item => item.PublishedAt >= start && item.PublishedAt < end).ToList();
    }
}
