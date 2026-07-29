using QueenZone.Web.Archive;

namespace QueenZone.Web.Tests;

public sealed class ArchivePageLoaderTests
{
    // Helpers ----------------------------------------------------------------

    private static Func<int, int, ArchivePageContext> SimpleContext =>
        (page, total) => new ArchivePageContext(
            page, total,
            $"Title page {page}",
            page == 1 ? "/test" : $"/test/page/{page}",
            page > 1 ? (page == 2 ? "/test" : $"/test/page/{page - 1}") : null,
            total > 0 && page < total ? $"/test/page/{page + 1}" : null);

    private static Func<int, int, int, int, int> PassThrough =>
        (_, _, _, rawTotal) => rawTotal;

    private static Task<IReadOnlyList<string>> Items(params string[] items) =>
        Task.FromResult<IReadOnlyList<string>>([.. items]);

    // page guard -------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99)]
    public async Task LoadAsync_ReturnsNotFound_WhenPageIsLessThanOne(int page)
    {
        var result = await ArchivePageLoader.LoadAsync(
            page,
            pageSize: 10,
            getCount: _ => Task.FromResult(100),
            getPage: (_, _, _) => Items("item"),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        Assert.IsType<ArchivePageResult<string>.NotFound>(result);
    }

    // empty archive ----------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReturnsSuccess_WhenArchiveIsEmpty_OnPageOne()
    {
        var result = await ArchivePageLoader.LoadAsync(
            page: 1,
            pageSize: 10,
            getCount: _ => Task.FromResult(0),
            getPage: (_, _, _) => Items(),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        var success = Assert.IsType<ArchivePageResult<string>.Success>(result);
        Assert.Empty(success.Items);
        Assert.Equal(1, success.Context.CurrentPage);
        Assert.Equal(0, success.Context.TotalPages);
    }

    [Fact]
    public async Task LoadAsync_ReturnsNotFound_WhenArchiveIsEmpty_OnPageTwo()
    {
        var result = await ArchivePageLoader.LoadAsync(
            page: 2,
            pageSize: 10,
            getCount: _ => Task.FromResult(0),
            getPage: (_, _, _) => Items(),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        Assert.IsType<ArchivePageResult<string>.NotFound>(result);
    }

    // page beyond total ------------------------------------------------------

    [Theory]
    [InlineData(3, 20, 10)]  // 20 items / 10 per page = 2 pages; page 3 is beyond
    [InlineData(5, 5, 5)]    // 1 page; page 5 is beyond
    public async Task LoadAsync_ReturnsNotFound_WhenPageExceedsTotalPages(int page, int count, int pageSize)
    {
        var result = await ArchivePageLoader.LoadAsync(
            page,
            pageSize,
            getCount: _ => Task.FromResult(count),
            getPage: (_, _, _) => Items(),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        Assert.IsType<ArchivePageResult<string>.NotFound>(result);
    }

    // success ----------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReturnsSuccess_WithCorrectItemsAndContext()
    {
        var result = await ArchivePageLoader.LoadAsync(
            page: 2,
            pageSize: 10,
            getCount: _ => Task.FromResult(25),  // 3 pages
            getPage: (_, _, _) => Items("a", "b", "c"),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        var success = Assert.IsType<ArchivePageResult<string>.Success>(result);
        Assert.Equal(["a", "b", "c"], success.Items);
        Assert.Equal(2, success.Context.CurrentPage);
        Assert.Equal(3, success.Context.TotalPages);
    }

    [Fact]
    public async Task LoadAsync_PassesCorrectPageAndSizeToGetPage()
    {
        int capturedPage = 0, capturedSize = 0;

        await ArchivePageLoader.LoadAsync(
            page: 4,
            pageSize: 7,
            getCount: _ => Task.FromResult(100),
            getPage: (p, size, ct) =>
            {
                capturedPage = p;
                capturedSize = size;
                return Items("x");
            },
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        Assert.Equal(4, capturedPage);
        Assert.Equal(7, capturedSize);
    }

    // resolveTotalPages ------------------------------------------------------

    [Fact]
    public async Task LoadAsync_PassesCorrectArgumentsToResolveTotalPages()
    {
        int capturedPage = 0, capturedItemCount = 0, capturedPublishedCount = 0, capturedRawTotal = 0;

        await ArchivePageLoader.LoadAsync(
            page: 3,
            pageSize: 5,
            getCount: _ => Task.FromResult(17),     // raw total pages = ceil(17/5) = 4
            getPage: (_, _, _) => Items("x", "y"),  // 2 items
            resolveTotalPages: (p, ic, pc, raw) =>
            {
                capturedPage = p;
                capturedItemCount = ic;
                capturedPublishedCount = pc;
                capturedRawTotal = raw;
                return raw;
            },
            buildContext: SimpleContext,
            ct: default);

        Assert.Equal(3, capturedPage);
        Assert.Equal(2, capturedItemCount);
        Assert.Equal(17, capturedPublishedCount);
        Assert.Equal(4, capturedRawTotal);
    }

    [Fact]
    public async Task LoadAsync_UsesResolvedTotalPages_NotRaw()
    {
        // resolveTotalPages returns 5 regardless of raw; page 4 is within range
        var result = await ArchivePageLoader.LoadAsync(
            page: 4,
            pageSize: 10,
            getCount: _ => Task.FromResult(10),  // raw = 1 page
            getPage: (_, _, _) => Items("z"),
            resolveTotalPages: (_, _, _, _) => 5,
            buildContext: SimpleContext,
            ct: default);

        // page 4 <= resolved total (5), so should succeed
        var success = Assert.IsType<ArchivePageResult<string>.Success>(result);
        Assert.Equal(5, success.Context.TotalPages);
    }

    // prev / next paths ------------------------------------------------------

    [Fact]
    public async Task LoadAsync_NoPrevPath_OnFirstPage()
    {
        var result = await ArchivePageLoader.LoadAsync(
            page: 1,
            pageSize: 10,
            getCount: _ => Task.FromResult(30),
            getPage: (_, _, _) => Items("a"),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        var success = Assert.IsType<ArchivePageResult<string>.Success>(result);
        Assert.Null(success.Context.PrevPath);
        Assert.NotNull(success.Context.NextPath);
    }

    [Fact]
    public async Task LoadAsync_NoNextPath_OnLastPage()
    {
        var result = await ArchivePageLoader.LoadAsync(
            page: 3,
            pageSize: 10,
            getCount: _ => Task.FromResult(30),  // exactly 3 pages
            getPage: (_, _, _) => Items("a"),
            resolveTotalPages: PassThrough,
            buildContext: SimpleContext,
            ct: default);

        var success = Assert.IsType<ArchivePageResult<string>.Success>(result);
        Assert.NotNull(success.Context.PrevPath);
        Assert.Null(success.Context.NextPath);
    }
}
