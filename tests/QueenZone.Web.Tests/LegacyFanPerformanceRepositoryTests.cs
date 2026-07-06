using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class LegacyFanPerformanceRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_Throws_WhenDatabaseIsUnreachable()
    {
        var repository = new LegacyFanPerformanceRepository(
            "Server=127.0.0.1,1;Database=QueenZone;User Id=invalid;Password=invalid;Encrypt=True;TrustServerCertificate=True;Connection Timeout=1");

        await Assert.ThrowsAnyAsync<Exception>(() => repository.GetByIdAsync(187));
    }
}
