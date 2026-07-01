using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryForumRepositorySearchTests
{
    private static InMemoryForumRepository CreateRepository() =>
        new(SampleForumData.CreateSeedCategories(), SampleForumData.CreateSeedStats());

    [Fact]
    public async Task SearchReturnsResultsMatchingThreadTitle()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync("studio album", 1, 20);

        Assert.NotEmpty(page.Results);
        Assert.All(page.Results, r => Assert.Contains("studio album", r.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchIsCaseInsensitive()
    {
        var repo = CreateRepository();

        var lower = await repo.SearchForumAsync("ranking", 1, 20);
        var upper = await repo.SearchForumAsync("RANKING", 1, 20);

        Assert.Equal(lower.TotalCount, upper.TotalCount);
    }

    [Fact]
    public async Task SearchReturnsEmptyForNoMatch()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync("xyzzy_no_match_zzzqq", 1, 20);

        Assert.Empty(page.Results);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task SearchReturnsEmptyForBlankQuery()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync("   ", 1, 20);

        Assert.Empty(page.Results);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task SearchResultIncludesCategoryInfo()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync("studio album", 1, 20);

        var result = Assert.Single(page.Results);
        Assert.Equal(1, result.CategoryId);
        Assert.Equal("The Music", result.CategoryName);
        Assert.True(result.ReplyCount >= 0);
    }

    [Fact]
    public async Task SearchRespectsPagination()
    {
        var repo = CreateRepository();

        var pageOne = await repo.SearchForumAsync("archive", 1, 2);
        var pageTwo = await repo.SearchForumAsync("archive", 2, 2);

        Assert.Equal(2, pageOne.Results.Count);
        Assert.True(pageOne.TotalCount > 2);
        Assert.NotEqual(
            pageOne.Results[0].TopicId,
            pageTwo.Results[0].TopicId);
    }
}
