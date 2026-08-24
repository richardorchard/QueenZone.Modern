namespace QueenZone.Data;

/// <summary>
/// Optional date-range filter for <see cref="INewsRepository.GetArchivePageAsync"/> and
/// <see cref="INewsRepository.GetPublishedCountAsync"/>: either a decade (10-year span, from the
/// decade chips) or a single calendar year (1-year span, from the year-rail scrubber, issue #886).
/// </summary>
/// <remarks>
/// News runs roughly 2006-present, unlike the older biography/discography timelines. Filtering
/// client-side over whatever page happens to be loaded produces false "no articles" results for
/// older decades (issue #838), so the date bound is pushed down to the repository/SQL layer
/// instead.
/// </remarks>
public readonly record struct NewsArchiveFilter(int? StartYear, int SpanYears = 10)
{
    public static readonly NewsArchiveFilter None = default;

    /// <summary>
    /// True when <see cref="StartYear"/>/<see cref="SpanYears"/> can be turned into a UTC
    /// <see cref="DateTime"/> window. Out-of-range years (0, negative, or a start that would
    /// overflow <see cref="DateTime.MaxValue"/>) stay inactive so public query strings cannot
    /// throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public bool IsActive => TryGetBounds(out _, out _);

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
        if (!TryCreateBounds(startYear, 10, out _, out _))
        {
            return None;
        }

        return new NewsArchiveFilter(startYear, 10);
    }

    /// <summary>
    /// Parses a <c>year</c> query value (e.g. from the year-rail scrubber, issue #886): an exact
    /// calendar year, not floored to a decade. Unsafe years become <see cref="None"/>, matching
    /// <see cref="Parse"/>.
    /// </summary>
    public static NewsArchiveFilter ParseYear(int? year)
    {
        if (year is not { } y)
        {
            return None;
        }

        if (y < DateTime.MinValue.Year || y > DateTime.MaxValue.Year)
        {
            return None;
        }

        if (!TryCreateBounds(y, 1, out _, out _))
        {
            return None;
        }

        return new NewsArchiveFilter(y, 1);
    }

    /// <summary>Inclusive start / exclusive end of the filter window, in UTC.</summary>
    public (DateTime Start, DateTime End) GetBounds()
    {
        if (!TryGetBounds(out var start, out var end))
        {
            throw new InvalidOperationException($"{nameof(GetBounds)} requires an active filter.");
        }

        return (start, end);
    }

    public bool TryGetBounds(out DateTime start, out DateTime end)
    {
        if (StartYear is not { } year)
        {
            start = default;
            end = default;
            return false;
        }

        return TryCreateBounds(year, SpanYears, out start, out end);
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

/// <summary>In-memory equivalent of the SQL date-range WHERE clause, shared by the sample/in-memory repositories.</summary>
internal static class NewsArchiveFiltering
{
    public static IReadOnlyList<NewsItem> Apply(IReadOnlyList<NewsItem> items, NewsArchiveFilter filter)
    {
        if (!filter.TryGetBounds(out var start, out var end))
        {
            return items;
        }

        return items.Where(item => item.PublishedAt >= start && item.PublishedAt < end).ToList();
    }
}
