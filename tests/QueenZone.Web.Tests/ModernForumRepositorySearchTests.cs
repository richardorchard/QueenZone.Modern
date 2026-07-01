using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ModernForumRepositorySearchTests
{
    private static ModernForumRepository CreateRepository() =>
        new("Server=.;Database=QueenZone;Integrated Security=true;");

    [Fact]
    public async Task SearchForumAsync_ReturnsEmptyPageForBlankQuery()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync("   ", 1, 20);

        Assert.Empty(page.Results);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    [Fact]
    public async Task SearchForumAsync_ReturnsEmptyPageForNullQuery()
    {
        var repo = CreateRepository();

        var page = await repo.SearchForumAsync(null!, 1, 20);

        Assert.Empty(page.Results);
        Assert.Equal(0, page.TotalCount);
    }
}
