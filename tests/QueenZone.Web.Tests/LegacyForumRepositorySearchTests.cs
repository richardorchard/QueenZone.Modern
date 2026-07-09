using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyForumRepositorySearchTests
{
    [Fact]
    public async Task SearchForumAsync_ThrowsNotSupportedException()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer("Server=.;Database=QueenZone;Integrated Security=true;Connect Timeout=1")
            .Options;
        var repo = new LegacyForumRepository(new QueenZoneDbContext(options));

        await Assert.ThrowsAsync<NotSupportedException>(() => repo.SearchForumAsync("queen", 1, 20));
    }
}
