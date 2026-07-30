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

    [Fact]
    public async Task Admin_news_full_lifecycle_probe_creates_edits_publishes_unpublishes_and_deletes_when_enabled()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        await using var provider = CreateProvider(connectionString);
        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var editorEmail = "legacy-write-probe@queenzone.local";
        var initialDraft = new AdminNewsDraft(
            $"Full lifecycle live probe {uniqueSuffix}",
            $"full-lifecycle-live-probe-{uniqueSuffix}",
            "Full lifecycle probe initial excerpt.",
            "Full lifecycle probe initial body.",
            new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            "https://www.queenonline.com/news/full-lifecycle-live-probe");
        var editedDraft = initialDraft with
        {
            Title = $"Full lifecycle live probe edited {uniqueSuffix}",
            Slug = $"full-lifecycle-live-probe-edited-{uniqueSuffix}",
            Excerpt = "Full lifecycle probe edited excerpt.",
            Body = "Full lifecycle probe edited body."
        };

        int? newsId = null;
        try
        {
            await using (var createScope = provider.CreateAsyncScope())
            {
                var adminRepository = createScope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
                var newsRepository = createScope.ServiceProvider.GetRequiredService<INewsRepository>();
                var auditRepository = createScope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

                newsId = await adminRepository.CreateDraftAsync(initialDraft, editorEmail);
                await auditRepository.AppendAsync(newsId.Value, "create-probe", editorEmail, "Full lifecycle live probe created draft.");

                var created = await adminRepository.GetByIdAsync(newsId.Value);
                Assert.NotNull(created);
                Assert.False(created.IsPublished);
                Assert.Equal(initialDraft.Title, created.Title);
                Assert.Null(await newsRepository.GetByIdAsync(newsId.Value));
                var initialSlug = NewsSlug.Resolve(initialDraft.Title, initialDraft.Slug);
                Assert.True(await adminRepository.IsSlugInUseAsync(initialSlug));
                Assert.False(await adminRepository.IsSlugInUseAsync(initialSlug, newsId.Value));
            }

            await using (var editScope = provider.CreateAsyncScope())
            {
                var adminRepository = editScope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
                var newsRepository = editScope.ServiceProvider.GetRequiredService<INewsRepository>();
                var auditRepository = editScope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

                await adminRepository.UpdateAsync(newsId.Value, editedDraft, editorEmail);
                await auditRepository.AppendAsync(newsId.Value, "edit-probe", editorEmail, "Full lifecycle live probe edited draft.");

                var edited = await adminRepository.GetByIdAsync(newsId.Value);
                Assert.NotNull(edited);
                Assert.False(edited.IsPublished);
                Assert.Equal(editedDraft.Title, edited.Title);
                Assert.Equal(NewsSlug.Resolve(editedDraft.Title, editedDraft.Slug), edited.Slug);
                Assert.Null(await newsRepository.GetByIdAsync(newsId.Value));
            }

            await using (var publishScope = provider.CreateAsyncScope())
            {
                var adminRepository = publishScope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
                var newsRepository = publishScope.ServiceProvider.GetRequiredService<INewsRepository>();
                var auditRepository = publishScope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

                var beforePublishDate = DateTime.UtcNow.Date;
                await adminRepository.PublishAsync(newsId.Value, editorEmail);
                await auditRepository.AppendAsync(newsId.Value, "publish-probe", editorEmail, "Full lifecycle live probe published draft.");

                var adminPublished = await adminRepository.GetByIdAsync(newsId.Value);
                Assert.NotNull(adminPublished);
                Assert.True(adminPublished.IsPublished);
                Assert.InRange(adminPublished.PublishedAt.Date, beforePublishDate, DateTime.UtcNow.Date);

                var publicPublished = await newsRepository.GetByIdAsync(newsId.Value);
                Assert.NotNull(publicPublished);
                Assert.True(publicPublished.IsPublished);
                Assert.Equal(editedDraft.Title, publicPublished.Title);
                Assert.Equal(editedDraft.Body, publicPublished.Body);
                Assert.InRange(publicPublished.PublishedAt.Date, beforePublishDate, DateTime.UtcNow.Date);

                var sitemapEntries = await newsRepository.GetPublishedSitemapEntriesAsync();
                Assert.Contains(sitemapEntries, entry => entry.Id == newsId.Value && entry.Title == editedDraft.Title);
            }

            await using (var unpublishScope = provider.CreateAsyncScope())
            {
                var adminRepository = unpublishScope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
                var newsRepository = unpublishScope.ServiceProvider.GetRequiredService<INewsRepository>();
                var auditRepository = unpublishScope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

                await adminRepository.UnpublishAsync(newsId.Value, editorEmail);
                await auditRepository.AppendAsync(newsId.Value, "unpublish-probe", editorEmail, "Full lifecycle live probe unpublished draft.");

                var adminUnpublished = await adminRepository.GetByIdAsync(newsId.Value);
                Assert.NotNull(adminUnpublished);
                Assert.False(adminUnpublished.IsPublished);
                Assert.Null(await newsRepository.GetByIdAsync(newsId.Value));

                var auditEntries = await auditRepository.GetByNewsIdAsync(newsId.Value);
                Assert.Contains(auditEntries, entry => entry.Action == "create-probe");
                Assert.Contains(auditEntries, entry => entry.Action == "edit-probe");
                Assert.Contains(auditEntries, entry => entry.Action == "publish-probe");
                Assert.Contains(auditEntries, entry => entry.Action == "unpublish-probe");
            }
        }
        finally
        {
            if (newsId is int id)
            {
                await CleanupLiveProbeArticleAsync(provider, id, editorEmail);
            }
        }
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

    private static async Task CleanupLiveProbeArticleAsync(
        ServiceProvider provider,
        int newsId,
        string editorEmail)
    {
        await using var cleanupScope = provider.CreateAsyncScope();
        var dbContext = cleanupScope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();
        var adminRepository = cleanupScope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();

        try
        {
            if (await adminRepository.GetByIdAsync(newsId) is not null)
            {
                await adminRepository.DeleteAsync(newsId, editorEmail);
            }
        }
        finally
        {
            await dbContext.NewsAuditLogs
                .Where(entry => entry.NewsId == newsId)
                .ExecuteDeleteAsync();

            Assert.Null(await adminRepository.GetByIdAsync(newsId));
            Assert.False(await dbContext.NewsAuditLogs.AnyAsync(entry => entry.NewsId == newsId));
        }
    }
}
