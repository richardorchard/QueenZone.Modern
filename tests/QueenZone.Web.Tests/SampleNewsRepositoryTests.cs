using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SampleNewsRepositoryTests
{
    private readonly SampleNewsRepository repository = new();

    [Fact]
    public async Task GetByIdAsync_returns_sample_article_with_rich_html_body()
    {
        var item = await repository.GetByIdAsync(1003);

        Assert.NotNull(item);
        Assert.Equal("QueenZone modernisation begins", item!.Title);
        Assert.Contains("<strong>ASP.NET Core</strong>", item.Body, StringComparison.Ordinal);
        Assert.Contains("href=\"https://www.queenzone.org/news\"", item.Body, StringComparison.Ordinal);
        Assert.Contains("src=\"/ugc/news/sample-crest.jpg\"", item.Body, StringComparison.Ordinal);
        Assert.Contains("alt=\"QueenZone crest\"", item.Body, StringComparison.Ordinal);
        Assert.Null(item.ImageBlobKey);
        Assert.Null(item.ImageGalleryPicId);
    }

    [Fact]
    public async Task GetByIdAsync_excludes_unpublished_sample_draft()
    {
        Assert.Null(await repository.GetByIdAsync(9001));
    }

    [Fact]
    public async Task GetByIdsAsync_returns_published_matches_and_omits_missing_and_unpublished()
    {
        var items = await repository.GetByIdsAsync([1003, 9001, 1003, 404, 1001]);

        Assert.Equal([1003, 1001], items.Select(item => item.Id).ToArray());
        Assert.Empty(await repository.GetByIdsAsync([]));
    }

    [Fact]
    public async Task SearchAsync_matches_rich_html_body_text()
    {
        var page = await repository.SearchAsync("sample-crest", 1, 20);

        Assert.Contains(page.Items, item => item.Id == 1003);
    }

    [Fact]
    public async Task GetArchiveYearRangeAsync_excludes_unpublished_items()
    {
        var range = await repository.GetArchiveYearRangeAsync();

        Assert.Equal(2026, range.MinYear);
        Assert.Equal(2026, range.MaxYear);
    }
}
