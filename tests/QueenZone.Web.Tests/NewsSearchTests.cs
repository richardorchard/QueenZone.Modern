using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

// ---------------------------------------------------------------------------
// InMemory repository: unit tests for the in-memory LINQ search path
// ---------------------------------------------------------------------------

public sealed class InMemoryNewsSearchTests
{
    private static InMemoryNewsRepository CreateRepository() =>
        new(SampleNewsData.CreateSeedArticles()
            .Select(a => new NewsItem(
                a.Id, a.Title, a.Excerpt, a.Body, a.PublishedAt, a.SourceUrl, a.IsPublished,
                string.IsNullOrWhiteSpace(a.Slug) ? null : a.Slug)));

    [Fact]
    public async Task SearchReturnsResultsMatchingTitle()
    {
        var repo = CreateRepository();

        var page = await repo.SearchAsync("modernisation", 1, 20);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item =>
            Assert.True(
                item.Title.Contains("modernisation", StringComparison.OrdinalIgnoreCase) ||
                item.Excerpt.Contains("modernisation", StringComparison.OrdinalIgnoreCase) ||
                item.Body.Contains("modernisation", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task SearchIsCaseInsensitive()
    {
        var repo = CreateRepository();

        var lower = await repo.SearchAsync("modernisation", 1, 20);
        var upper = await repo.SearchAsync("MODERNISATION", 1, 20);

        Assert.Equal(lower.TotalCount, upper.TotalCount);
    }

    [Fact]
    public async Task SearchReturnsEmptyForNoMatch()
    {
        var repo = CreateRepository();

        var page = await repo.SearchAsync("xyzzy_no_match_zzzqq", 1, 20);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task SearchReturnsEmptyForBlankQuery()
    {
        var repo = CreateRepository();

        var page = await repo.SearchAsync("   ", 1, 20);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task SearchNeverReturnsHiddenOrDraftRecords()
    {
        var repo = CreateRepository();

        var page = await repo.SearchAsync("moderation", 1, 20);

        Assert.Empty(page.Items);
        Assert.DoesNotContain(page.Items, item => item.Title.Contains("Hidden", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchMatchesExcerpt()
    {
        var repo = CreateRepository();

        // ID 1002 excerpt mentions "canonical routes"
        var page = await repo.SearchAsync("canonical routes", 1, 20);

        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task SearchMatchesBody()
    {
        var repo = CreateRepository();

        // SampleNewsData bodies contain "Body for archive sample article {id}"
        var page = await repo.SearchAsync("Body for archive sample", 1, 20);

        Assert.NotEmpty(page.Items);
    }

    [Fact]
    public async Task SearchRespectsPagination()
    {
        var repo = CreateRepository();

        // "archive" matches IDs 1002 and 1004–1022 (many items)
        var pageOne = await repo.SearchAsync("archive", 1, 3);
        var pageTwo = await repo.SearchAsync("archive", 2, 3);

        Assert.Equal(3, pageOne.Items.Count);
        Assert.True(pageOne.TotalCount > 3);
        Assert.NotEqual(pageOne.Items[0].Id, pageTwo.Items[0].Id);
    }

    [Fact]
    public async Task SearchTotalCountReflectsAllMatches()
    {
        var repo = CreateRepository();

        var allResults = await repo.SearchAsync("archive", 1, 100);
        var pagedResults = await repo.SearchAsync("archive", 1, 3);

        Assert.Equal(allResults.TotalCount, pagedResults.TotalCount);
        Assert.True(pagedResults.TotalCount > pagedResults.Items.Count);
    }
}

// ---------------------------------------------------------------------------
// EfNewsRepository: SQLite path tests for the LIKE fallback used in tests
// ---------------------------------------------------------------------------

public sealed class EfNewsRepositorySearchTests : IAsyncDisposable
{
    // SQLite-compatible LIKE search SQL. Mirrors the shape of EfProductionSql.CreateNewsSqliteLikeSearchQueries
    // but uses LIMIT/OFFSET syntax instead of SQL Server's OFFSET/FETCH.
    private const string SqliteSearchSql = """
        SELECT
            NEWS_ID  AS Id,
            COALESCE(TITLE, '')   AS Title,
            COALESCE(EXCERPT, '') AS Excerpt,
            ''                    AS Body,
            "DATE"  AS PublishedAt,
            SOURCE_URL AS SourceUrl,
            DISPLAY    AS IsPublished,
            SLUG       AS Slug
        FROM NEWS_T
        WHERE DISPLAY = 1
          AND (TITLE LIKE {0} OR EXCERPT LIKE {0} OR ARTICLE LIKE {0})
        ORDER BY "DATE" DESC, NEWS_ID DESC
        LIMIT {2} OFFSET {1}
        """;

    private const string SqliteSearchCountSql = """
        SELECT COUNT(*) AS Value
        FROM NEWS_T
        WHERE DISPLAY = 1
          AND (TITLE LIKE {0} OR EXCERPT LIKE {0} OR ARTICLE LIKE {0})
        """;

    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfNewsRepository repository;

    public EfNewsRepositorySearchTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        AdminNewsSqliteTestHarness.EnsureNewsTable(dbContext);

        // Seed: one published article matching "bohemian rhapsody"
        AdminNewsSqliteTestHarness.SeedArticle(
            dbContext, 1, "Bohemian Rhapsody release", "Classic Queen single", "Full article body", "2026-01-01", isPublished: true);
        // Second published article – different keyword; both share "Queen" for pagination tests
        AdminNewsSqliteTestHarness.SeedArticle(
            dbContext, 2, "Live Aid 1985", "The greatest show", "Mercury stole the Queen show", "2026-01-02", isPublished: true);
        // Unpublished – should never appear in results
        AdminNewsSqliteTestHarness.SeedArticle(
            dbContext, 3, "Hidden draft about bohemian", "Secret excerpt", "Secret body", "2026-01-03", isPublished: false);

        repository = new EfNewsRepository(
            dbContext,
            latestSql: string.Empty,
            countSql: string.Empty,
            archivePageSql: string.Empty,
            byIdSql: string.Empty,
            sitemapSql: string.Empty,
            sqliteLikeSearchSql: SqliteSearchSql,
            sqliteLikeSearchCountSql: SqliteSearchCountSql);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SearchAsync_returns_empty_for_blank_query()
    {
        var result = await repository.SearchAsync("   ", 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_matches_title()
    {
        var result = await repository.SearchAsync("Bohemian Rhapsody", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_matches_excerpt()
    {
        var result = await repository.SearchAsync("greatest show", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }

    [Fact]
    public async Task SearchAsync_matches_body()
    {
        var result = await repository.SearchAsync("Mercury stole", 1, 20);

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }

    [Fact]
    public async Task SearchAsync_is_case_insensitive()
    {
        var lower = await repository.SearchAsync("bohemian rhapsody", 1, 20);
        var upper = await repository.SearchAsync("BOHEMIAN RHAPSODY", 1, 20);

        Assert.Equal(lower.TotalCount, upper.TotalCount);
        Assert.Equal(1, lower.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_returns_empty_for_no_match()
    {
        var result = await repository.SearchAsync("xyzzy_nothing", 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_never_returns_unpublished_records()
    {
        // "bohemian" matches both ID 1 (published) and ID 3 (unpublished)
        var result = await repository.SearchAsync("bohemian", 1, 20);

        Assert.DoesNotContain(result.Items, item => item.Id == 3);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_paginates_correctly()
    {
        // Both published articles contain "Queen"
        var pageOne = await repository.SearchAsync("Queen", 1, 1);
        var pageTwo = await repository.SearchAsync("Queen", 2, 1);

        Assert.Single(pageOne.Items);
        Assert.Single(pageTwo.Items);
        Assert.Equal(2, pageOne.TotalCount);
        Assert.NotEqual(pageOne.Items[0].Id, pageTwo.Items[0].Id);
    }

    [Fact]
    public async Task SearchAsync_respects_page_size()
    {
        var result = await repository.SearchAsync("Queen", 1, 1);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
    }
}

// ---------------------------------------------------------------------------
// Web integration tests for /news/search route
// ---------------------------------------------------------------------------

public sealed class NewsSearchRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public NewsSearchRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task NewsSearchPage_renders_empty_state_without_query()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/search");

        Assert.Contains("Search News", body);
        Assert.DoesNotContain("results for", body);
    }

    [Fact]
    public async Task NewsSearchPage_returns_results_for_known_keyword()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/search?q=modernisation");

        Assert.Contains("1 result", body);
        Assert.Contains("QueenZone modernisation begins", body);
        Assert.Contains("/news/1003/", body);
    }

    [Fact]
    public async Task NewsSearchPage_shows_no_results_state_for_unknown_keyword()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/search?q=xyzzy_no_match_zzzqq");

        Assert.Contains("No results found", body);
        Assert.DoesNotContain("qz-news-list", body);
    }

    [Fact]
    public async Task NewsSearchPage_never_exposes_hidden_records()
    {
        var client = factory.CreateClient();

        // "moderation" appears in the unpublished seed record's body
        var body = await client.GetStringAsync("/news/search?q=moderation");

        Assert.DoesNotContain("Hidden moderation draft", body);
    }

    [Fact]
    public async Task NewsSearchPage_is_reachable_and_returns_ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/news/search?q=archive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NewsSearchPage_has_search_form_pointing_to_news_search()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news/search");

        Assert.Contains("action=\"/news/search\"", body);
    }

    [Fact]
    public async Task NewsIndex_has_search_form_pointing_to_news_search()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/news");

        Assert.Contains("action=\"/news/search\"", body);
    }
}
