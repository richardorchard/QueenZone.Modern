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

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
