using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfAdminNewsRepositoryLegacyWriteProbeTests
{
    [Fact]
    public async Task Write_probe_creates_publishes_unpublishes_and_deletes_when_connection_configured()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfAdminNewsRepository(dbContext);
        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var draft = new AdminNewsDraft(
            $"Probe write {uniqueSuffix}",
            $"probe-write-{uniqueSuffix}",
            "Automated legacy write probe excerpt.",
            "Automated legacy write probe body.",
            DateTime.UtcNow.Date,
            null);

        var newsId = await repository.CreateDraftAsync(draft, "legacy-write-probe@queenzone.local");
        try
        {
            var created = await repository.GetByIdAsync(newsId);
            Assert.NotNull(created);
            Assert.Equal(draft.Title, created.Title);
            Assert.False(created.IsPublished);

            await repository.PublishAsync(newsId, "legacy-write-probe@queenzone.local");
            var published = await repository.GetByIdAsync(newsId);
            Assert.NotNull(published);
            Assert.True(published.IsPublished);

            await repository.UnpublishAsync(newsId, "legacy-write-probe@queenzone.local");
            var unpublished = await repository.GetByIdAsync(newsId);
            Assert.NotNull(unpublished);
            Assert.False(unpublished.IsPublished);
        }
        finally
        {
            await repository.DeleteAsync(newsId, "legacy-write-probe@queenzone.local");
            Assert.Null(await repository.GetByIdAsync(newsId));
        }
    }

    private static bool IsWriteProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_LEGACY_WRITE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
