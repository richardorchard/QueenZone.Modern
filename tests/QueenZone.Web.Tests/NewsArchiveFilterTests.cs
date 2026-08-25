using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsArchiveFilterTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(9995, null)]
    [InlineData(5, null)]
    [InlineData(2000, 2000)]
    [InlineData(2010, 2010)]
    [InlineData(2015, 2010)]
    [InlineData(2026, 2020)]
    public void Parse_floors_valid_years_and_ignores_unsafe_years(int? input, int? expectedDecadeStart)
    {
        var filter = NewsArchiveFilter.Parse(input);

        Assert.Equal(expectedDecadeStart, filter.DecadeStartYear);
        Assert.Equal(expectedDecadeStart is not null, filter.IsActive);
    }

    [Fact]
    public void GetDecadeBounds_returns_inclusive_exclusive_utc_window()
    {
        var (start, end) = new NewsArchiveFilter(2000).GetDecadeBounds();

        Assert.Equal(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Utc), end);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9995)]
    public void Unsafe_constructed_years_are_inactive_and_do_not_throw_argument_out_of_range(int year)
    {
        var filter = new NewsArchiveFilter(year);

        Assert.False(filter.IsActive);
        Assert.False(filter.TryGetDecadeBounds(out _, out _));
        Assert.Throws<InvalidOperationException>(() => filter.GetDecadeBounds());
    }

    [Fact]
    public void Apply_returns_original_list_when_filter_is_inactive()
    {
        var items = new[]
        {
            new NewsItem(1, "A", "Ex", "Body", new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true),
        };

        Assert.Same(items, NewsArchiveFiltering.Apply(items, NewsArchiveFilter.None));
        Assert.Same(items, NewsArchiveFiltering.Apply(items, new NewsArchiveFilter(0)));
    }

    [Fact]
    public void Apply_keeps_items_inside_the_decade_window()
    {
        var inWindow = new NewsItem(1, "In", "Ex", "Body", new DateTime(2008, 3, 4, 0, 0, 0, DateTimeKind.Utc), null, true);
        var outside = new NewsItem(2, "Out", "Ex", "Body", new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true);

        var applied = NewsArchiveFiltering.Apply([inWindow, outside], new NewsArchiveFilter(2000));

        Assert.Equal([inWindow], applied);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(-1, null)]
    [InlineData(9999, null)]
    [InlineData(2008, 2008)]
    public void Parse_year_ignores_unsafe_years(int? input, int? expectedYear)
    {
        var filter = NewsArchiveFilter.Parse(decade: null, year: input);

        Assert.Equal(expectedYear, filter.Year);
        Assert.Null(filter.DecadeStartYear);
        Assert.Equal(expectedYear is not null, filter.IsActive);
    }

    [Fact]
    public void Parse_prefers_year_over_decade_when_both_are_supplied()
    {
        var filter = NewsArchiveFilter.Parse(decade: 2010, year: 2008);

        Assert.Equal(2008, filter.Year);
        Assert.Null(filter.DecadeStartYear);
    }

    [Fact]
    public void GetDecadeBounds_returns_a_one_year_window_for_a_year_filter()
    {
        var (start, end) = new NewsArchiveFilter(null, 2008).GetDecadeBounds();

        Assert.Equal(new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.Equal(new DateTime(2009, 1, 1, 0, 0, 0, DateTimeKind.Utc), end);
    }

    [Fact]
    public void Apply_keeps_items_inside_the_year_window()
    {
        var inWindow = new NewsItem(1, "In", "Ex", "Body", new DateTime(2008, 12, 31, 0, 0, 0, DateTimeKind.Utc), null, true);
        var outside = new NewsItem(2, "Out", "Ex", "Body", new DateTime(2009, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, true);

        var applied = NewsArchiveFiltering.Apply([inWindow, outside], new NewsArchiveFilter(null, 2008));

        Assert.Equal([inWindow], applied);
    }
}
