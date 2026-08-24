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

    /// <summary>
    /// True when <see cref="DecadeStartYear"/> can be turned into a UTC
    /// <see cref="DateTime"/> window. Out-of-range years (0, negative, or
    /// a start that would overflow <see cref="DateTime.MaxValue"/>) stay inactive
    /// so public query strings cannot throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public bool IsActive => TryGetDecadeBounds(out _, out _);

    /// <summary>
    /// Parses a <c>decade</c> query value the way <see cref="PhotoListFilter.Parse"/>
    /// and <c>ApiPagination.Normalize</c> treat invalid input: ignore it rather than 400.
    /// Years are floored to the decade start (2015 → 2010). Unsafe years become <see cref="None"/>.
    /// </summary>
    public static NewsArchiveFilter Parse(int? decade)
    {
        if (decade is not { } year)
        {
            return None;
        }

        if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
        {
            return None;
        }

        var startYear = (year / 10) * 10;
        if (!TryCreateDecadeBounds(startYear, out _, out _))
        {
            return None;
        }

        return new NewsArchiveFilter(startYear);
    }

    /// <summary>Inclusive start / exclusive end of the decade, in UTC.</summary>
    public (DateTime Start, DateTime End) GetDecadeBounds()
    {
        if (!TryGetDecadeBounds(out var start, out var end))
        {
            throw new InvalidOperationException($"{nameof(GetDecadeBounds)} requires an active filter.");
        }

        return (start, end);
    }

    public bool TryGetDecadeBounds(out DateTime start, out DateTime end)
    {
        if (DecadeStartYear is not { } year)
        {
            start = default;
            end = default;
            return false;
        }

        return TryCreateDecadeBounds(year, out start, out end);
    }

    private static bool TryCreateDecadeBounds(int startYear, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        // End is start+10; reject before constructing so DateTime never throws.
        if (startYear < DateTime.MinValue.Year || startYear > DateTime.MaxValue.Year - 10)
        {
            return false;
        }

        start = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        end = new DateTime(startYear + 10, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return true;
    }
}

/// <summary>In-memory equivalent of the SQL decade WHERE clause, shared by the sample/in-memory repositories.</summary>
internal static class NewsArchiveFiltering
{
    public static IReadOnlyList<NewsItem> Apply(IReadOnlyList<NewsItem> items, NewsArchiveFilter filter)
    {
        if (!filter.TryGetDecadeBounds(out var start, out var end))
        {
            return items;
        }

        return items.Where(item => item.PublishedAt >= start && item.PublishedAt < end).ToList();
    }
}
