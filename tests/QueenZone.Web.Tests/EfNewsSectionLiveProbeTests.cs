using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfNewsSectionLiveProbeTests
{
    [Fact]
    public async Task Public_news_probe_materializes_archive_detail_search_and_sitemap_when_connection_configured()
    {
        if (!TryGetConnectionString(out var connectionString))
        {
            return;
        }

        await using var provider = CreateProvider(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var newsRepository = scope.ServiceProvider.GetRequiredService<INewsRepository>();

        var count = await newsRepository.GetPublishedCountAsync();
        Assert.True(count > 0, "The live news archive should have published records.");

        var latest = await newsRepository.GetLatestAsync(5);
        Assert.NotEmpty(latest);
        Assert.All(latest, item => Assert.True(item.IsPublished));

        var archivePage = await newsRepository.GetArchivePageAsync(1, 20);
        Assert.NotEmpty(archivePage);
        Assert.All(archivePage, item => Assert.True(item.IsPublished));

        var detail = await newsRepository.GetByIdAsync(archivePage[0].Id);
        Assert.NotNull(detail);
        Assert.True(detail.IsPublished);
        Assert.Equal(archivePage[0].Id, detail.Id);
        Assert.False(string.IsNullOrWhiteSpace(detail.Body));

        var search = await newsRepository.SearchAsync("Queen", 1, 5);
        Assert.True(search.TotalCount >= search.Items.Count);
        Assert.NotEmpty(search.Items);

        var sitemapEntries = await newsRepository.GetPublishedSitemapEntriesAsync();
        Assert.NotEmpty(sitemapEntries);
        Assert.Contains(sitemapEntries, entry => entry.Id == detail.Id && entry.Title == detail.Title);
    }

    [Fact]
    public async Task Admin_news_write_probe_preserves_public_visibility_rules_and_rolls_back_when_enabled()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        await using var provider = CreateProvider(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();
        var adminRepository = scope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
        var newsRepository = scope.ServiceProvider.GetRequiredService<INewsRepository>();
        var auditRepository = scope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var draft = new AdminNewsDraft(
            $"News section live probe {uniqueSuffix}",
            $"news-section-live-probe-{uniqueSuffix}",
            "Rollback-only public visibility probe excerpt.",
            "Rollback-only public visibility probe body.",
            DateTime.UtcNow.Date,
            "https://www.queenonline.com/news/live-probe");

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var newsId = await adminRepository.CreateDraftAsync(draft, "legacy-write-probe@queenzone.local");
            var created = await adminRepository.GetByIdAsync(newsId);
            Assert.NotNull(created);
            Assert.False(created.IsPublished);
            Assert.Null(await newsRepository.GetByIdAsync(newsId));

            await adminRepository.PublishAsync(newsId, "legacy-write-probe@queenzone.local");
            await auditRepository.AppendAsync(newsId, "publish-probe", "legacy-write-probe@queenzone.local", "Rollback-only publish probe.");
            var published = await newsRepository.GetByIdAsync(newsId);
            Assert.NotNull(published);
            Assert.True(published.IsPublished);
            Assert.Equal(draft.Title, published.Title);

            await adminRepository.UnpublishAsync(newsId, "legacy-write-probe@queenzone.local");
            await auditRepository.AppendAsync(newsId, "unpublish-probe", "legacy-write-probe@queenzone.local", "Rollback-only unpublish probe.");
            Assert.Null(await newsRepository.GetByIdAsync(newsId));

            await transaction.RollbackAsync();
        });
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddQueenZoneLegacyData(connectionString);
        return services.BuildServiceProvider();
    }

    private static bool TryGetConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(connectionString);
    }

    private static bool IsWriteProbeEnabled(out string connectionString)
    {
        if (!TryGetConnectionString(out connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_LEGACY_WRITE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
