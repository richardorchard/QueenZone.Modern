using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfAdminNewsRepositoryTests : IAsyncDisposable
{
    internal const string SqliteLatestNewsSql = AdminNewsSqliteTestHarness.LatestNewsSql;

    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfAdminNewsRepository repository;

    public EfAdminNewsRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        AdminNewsSqliteTestHarness.EnsureNewsTable(dbContext);
        AdminNewsSqliteTestHarness.SeedArticle(dbContext, 4201, "SQLite admin article");
        repository = new EfAdminNewsRepository(dbContext, SqliteLatestNewsSql);
    }

    [Fact]
    public async Task GetAllAsync_materializes_FromSql_results()
    {
        var articles = await repository.GetAllAsync();

        Assert.Single(articles);
        Assert.Equal(4201, articles[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_materializes_FromSql_results_without_composing_SingleOrDefault()
    {
        var article = await repository.GetByIdAsync(4201);

        Assert.NotNull(article);
        Assert.Equal(4201, article.Id);
        Assert.Equal("SQLite admin article", article.Title);
        Assert.False(article.IsPublished);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_article_is_missing()
    {
        var article = await repository.GetByIdAsync(9999);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetPageAsync_returns_requested_slice_and_total_count()
    {
        for (var id = 4202; id <= 4210; id++)
        {
            var title = $"Article {id}";
            var publishedAt = $"2026-06-{id - 4200:D2}";
            dbContext.Database.ExecuteSql($"""
                INSERT INTO NEWS_T (NEWS_ID, TITLE, EXCERPT, ARTICLE, "DATE", DISPLAY, TYPE, QUEEN_ONLINE)
                VALUES ({id}, {title}, 'Excerpt', 'Body', {publishedAt}, 0, 0, 0);
                """);
        }

        var firstPage = await repository.GetPageAsync(1, 4);

        Assert.Equal(10, firstPage.TotalCount);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(4, firstPage.PageSize);
        Assert.Equal(4, firstPage.Items.Count);
        Assert.Equal(4210, firstPage.Items[0].Id);
        Assert.Equal(4207, firstPage.Items[^1].Id);

        var secondPage = await repository.GetPageAsync(2, 4);

        Assert.Equal(10, secondPage.TotalCount);
        Assert.Equal(4, secondPage.Items.Count);
        Assert.Equal(4206, secondPage.Items[0].Id);
        Assert.Equal(4203, secondPage.Items[^1].Id);
    }

    [Fact]
    public async Task UpdateAsync_persists_changes_readable_via_GetByIdAsync()
    {
        var draft = new AdminNewsDraft(
            "Updated sqlite title",
            "updated-sqlite-title",
            "Updated excerpt",
            "Updated body",
            new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            null);

        await repository.UpdateAsync(4201, draft, "editor@test.local");

        var article = await repository.GetByIdAsync(4201);
        Assert.NotNull(article);
        Assert.Equal("Updated sqlite title", article.Title);
        Assert.Equal("Updated excerpt", article.Excerpt);
        Assert.Equal("Updated body", article.Body);
    }

    [Fact]
    public async Task DeleteAsync_removes_article_from_GetByIdAsync()
    {
        await repository.DeleteAsync(4201, "editor@test.local");

        var article = await repository.GetByIdAsync(4201);
        Assert.Null(article);
    }

    [Fact]
    public async Task GetById_then_DeleteAsync_matches_admin_delete_workflow()
    {
        var loaded = await repository.GetByIdAsync(4201);
        Assert.NotNull(loaded);
        Assert.Equal("SQLite admin article", loaded.Title);

        await repository.DeleteAsync(4201, "editor@test.local");

        Assert.Null(await repository.GetByIdAsync(4201));
    }

    [Fact]
    public async Task PublishAsync_sets_display_flag()
    {
        await repository.PublishAsync(4201, "editor@test.local");

        var article = await repository.GetByIdAsync(4201);
        Assert.NotNull(article);
        Assert.True(article.IsPublished);
    }

    [Fact]
    public async Task UnpublishAsync_clears_display_flag()
    {
        dbContext.Database.ExecuteSql($"""
            UPDATE NEWS_T SET DISPLAY = 1 WHERE NEWS_ID = 4201;
            """);

        await repository.UnpublishAsync(4201, "editor@test.local");

        var article = await repository.GetByIdAsync(4201);
        Assert.NotNull(article);
        Assert.False(article.IsPublished);
    }

    [Fact]
    public async Task IsSlugInUseAsync_detects_existing_slug()
    {
        dbContext.Database.ExecuteSql($"""
            UPDATE NEWS_T SET SLUG = 'shared-slug' WHERE NEWS_ID = 4201;
            """);

        Assert.True(await repository.IsSlugInUseAsync("shared-slug"));
        Assert.False(await repository.IsSlugInUseAsync("shared-slug", excludeNewsId: 4201));
        Assert.False(await repository.IsSlugInUseAsync("unused-slug"));
    }

    [Fact]
    public async Task DeleteAsync_throws_when_foreign_key_blocks_delete()
    {
        dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS NEWS_REF_TEST (
                ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                NEWS_ID INTEGER NOT NULL,
                FOREIGN KEY (NEWS_ID) REFERENCES NEWS_T(NEWS_ID)
            );
            """);
        dbContext.Database.ExecuteSql($"""
            INSERT INTO NEWS_REF_TEST (NEWS_ID) VALUES (4201);
            """);

        await Assert.ThrowsAnyAsync<Exception>(
            async () => await repository.DeleteAsync(4201, "editor@test.local"));
    }

    [Fact]
    public async Task CreateDraftAsync_uses_database_generated_identity()
    {
        var draft = new AdminNewsDraft(
            "Created draft",
            "created-draft",
            "Created excerpt",
            "Created body",
            new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc),
            null);

        var newsId = await repository.CreateDraftAsync(draft, "editor@test.local");

        Assert.True(newsId > 4201);
        var article = await repository.GetByIdAsync(newsId);
        Assert.NotNull(article);
        Assert.Equal("Created draft", article.Title);
    }

    [Fact]
    public async Task CreateDraftAsync_preserves_source_url_within_validation_limit()
    {
        var sourceUrl = "https://www.queenonline.com/news/" + new string('a', 400);
        Assert.True(sourceUrl.Length <= NewsValidation.MaxSourceUrlLength);
        var draft = new AdminNewsDraft(
            "Created draft with source",
            "created-draft-with-source",
            "Created excerpt",
            "Created body",
            new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc),
            sourceUrl);

        var newsId = await repository.CreateDraftAsync(draft, "editor@test.local");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SOURCE_URL FROM NEWS_T WHERE NEWS_ID = $newsId";
        command.Parameters.AddWithValue("$newsId", newsId);
        var savedSourceUrl = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.Equal(sourceUrl, savedSourceUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
