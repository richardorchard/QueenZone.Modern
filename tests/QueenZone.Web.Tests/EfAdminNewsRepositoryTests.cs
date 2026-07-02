using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfAdminNewsRepositoryTests : IAsyncDisposable
{
    private const string SqliteLatestNewsSql = """
        SELECT
            NEWS_ID,
            TITLE,
            EXCERPT,
            ARTICLE,
            "DATE",
            "DATE" AS PublishedAt,
            CAST(NULL AS TEXT) AS SOURCE_URL,
            DISPLAY,
            CAST(NULL AS TEXT) AS SLUG,
            CAST(NULL AS TEXT) AS CREATED_AT,
            CAST(NULL AS TEXT) AS UPDATED_AT,
            CAST(NULL AS TEXT) AS EDITOR_EMAIL,
            CAST(NULL AS INTEGER) AS USER_ID,
            NEWS_ID AS NewsId,
            TYPE,
            QUEEN_ONLINE
        FROM NEWS_T
        WHERE 1 = 1
        """;

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
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE NEWS_T (
                NEWS_ID INTEGER NOT NULL PRIMARY KEY,
                TITLE TEXT NOT NULL,
                EXCERPT TEXT NOT NULL,
                ARTICLE TEXT NOT NULL,
                "DATE" TEXT NOT NULL,
                DISPLAY INTEGER NOT NULL,
                USER_ID INTEGER NULL,
                TYPE INTEGER NOT NULL DEFAULT 0,
                QUEEN_ONLINE INTEGER NOT NULL DEFAULT 0
            );
            """);
        dbContext.Database.ExecuteSqlRaw("""
            INSERT INTO NEWS_T (NEWS_ID, TITLE, EXCERPT, ARTICLE, "DATE", DISPLAY, TYPE, QUEEN_ONLINE)
            VALUES (4201, 'SQLite admin article', 'Excerpt', 'Body', '2026-06-01', 0, 0, 0);
            """);
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

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
