namespace QueenZone.Data;

/// <summary>
/// Optional decade or single-year filter for <see cref="INewsRepository.GetArchivePageAsync"/> and
/// <see cref="INewsRepository.GetPublishedCountAsync"/>.
/// </summary>
/// <remarks>
/// News runs roughly 2006-present, unlike the older biography/discography timelines. Filtering
/// client-side over whatever page happens to be loaded produces false "no articles" results for
/// older decades (issue #838), so the decade bound is pushed down to the repository/SQL layer
/// instead. <see cref="Year"/> reuses the same bounded-window approach for the mobile year-rail
/// scrubber (issue #886), where the user jumps to a specific year rather than a decade.
/// </remarks>
public readonly record struct NewsArchiveFilter(int? DecadeStartYear, int? Year = null)
{
    public static readonly NewsArchiveFilter None = default;

    /// <summary>
    /// True when the filter can be turned into a UTC <see cref="DateTime"/> window. Out-of-range
    /// years (0, negative, or a start that would overflow <see cref="DateTime.MaxValue"/>) stay
    /// inactive so public query strings cannot throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public bool IsActive => TryGetBounds(out _, out _);

    /// <summary>
    /// Parses <c>decade</c>/<c>year</c> query values the way <see cref="PhotoListFilter.Parse"/>
    /// and <c>ApiPagination.Normalize</c> treat invalid input: ignore it rather than 400. When both
    /// are supplied, <paramref name="year"/> wins since it is the more specific filter (the
    /// year-rail scrubber). Decade years are floored to the decade start (2015 → 2010). Unsafe
    /// years become <see cref="None"/>.
    /// </summary>
    public static NewsArchiveFilter Parse(int? decade, int? year = null)
    {
        if (year is { } exactYear)
        {
            if (exactYear < DateTime.MinValue.Year || exactYear > DateTime.MaxValue.Year)
            {
                return None;
            }

            return TryCreateBounds(exactYear, 1, out _, out _)
                ? new NewsArchiveFilter(null, exactYear)
                : None;
        }

        if (decade is not { } decadeYear)
        {
            return None;
        }

        if (decadeYear < DateTime.MinValue.Year || decadeYear > DateTime.MaxValue.Year)
        {
            return None;
        }

        var startYear = (decadeYear / 10) * 10;
        return TryCreateBounds(startYear, 10, out _, out _)
            ? new NewsArchiveFilter(startYear)
            : None;
    }

    /// <summary>Inclusive start / exclusive end of the decade or year, in UTC.</summary>
    public (DateTime Start, DateTime End) GetDecadeBounds()
    {
        if (!TryGetBounds(out var start, out var end))
        {
            throw new InvalidOperationException($"{nameof(GetDecadeBounds)} requires an active filter.");
        }

        return (start, end);
    }

    public bool TryGetDecadeBounds(out DateTime start, out DateTime end) => TryGetBounds(out start, out end);

    private bool TryGetBounds(out DateTime start, out DateTime end)
    {
        if (Year is { } exactYear)
        {
            return TryCreateBounds(exactYear, 1, out start, out end);
        }

        if (DecadeStartYear is { } decadeYear)
        {
            return TryCreateBounds(decadeYear, 10, out start, out end);
        }

        start = default;
        end = default;
        return false;
    }

    private static bool TryCreateBounds(int startYear, int spanYears, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        // End is start+span; reject before constructing so DateTime never throws.
        if (startYear < DateTime.MinValue.Year || startYear > DateTime.MaxValue.Year - spanYears)
        {
            return false;
        }

        start = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        end = new DateTime(startYear + spanYears, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return true;
    }
}

/// <summary>In-memory equivalent of the SQL decade/year WHERE clause, shared by the sample/in-memory repositories.</summary>
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
