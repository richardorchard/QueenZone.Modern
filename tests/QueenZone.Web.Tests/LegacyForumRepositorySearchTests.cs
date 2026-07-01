using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyForumRepositorySearchTests
{
    [Fact]
    public async Task SearchForumAsync_ThrowsNotSupportedException()
    {
        var repo = new LegacyForumRepository("Server=.;Database=QueenZone;Integrated Security=true;");

        await Assert.ThrowsAsync<NotSupportedException>(() => repo.SearchForumAsync("queen", 1, 20));
    }
}
