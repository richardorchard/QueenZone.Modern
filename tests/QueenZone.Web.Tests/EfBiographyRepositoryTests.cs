using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfBiographyRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfBiographyRepository repository;

    public EfBiographyRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.ExecuteSqlRaw(
            """
            CREATE TABLE BioList (
                BIOTITLE TEXT,
                Q_BIO_ID INTEGER NOT NULL,
                CREATE_DATE TEXT NOT NULL,
                TITLE TEXT,
                DISPLAY_SEQUENCE INTEGER NOT NULL,
                SUMMARY TEXT
            );
            CREATE TABLE BioDetail (
                Q_BIO_ID INTEGER NOT NULL,
                TITLE TEXT,
                SUMMARY TEXT,
                BIO_TEXT TEXT,
                DISPLAY_SEQUENCE INTEGER NOT NULL
            );
            """);

        repository = new EfBiographyRepository(
            dbContext,
            listSql: """
                SELECT BIOTITLE, Q_BIO_ID, CREATE_DATE, TITLE, DISPLAY_SEQUENCE, SUMMARY
                FROM BioList
                ORDER BY DISPLAY_SEQUENCE
                """,
            detailSql: id => $"""
                SELECT TITLE, SUMMARY, BIO_TEXT, DISPLAY_SEQUENCE
                FROM BioDetail
                WHERE Q_BIO_ID = {id}
                """);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetChaptersAsync_materializes_SqlQuery_rows_and_maps_fields()
    {
        SeedListRow(1, " Chapter One ", 2, "Summary one", "2020-01-15");
        SeedListRow(2, "Chapter Two", 1, null, "2019-06-01");

        var chapters = await repository.GetChaptersAsync();

        Assert.Equal(2, chapters.Count);
        // Seeded with display sequences 2 then 1; list SQL orders by DISPLAY_SEQUENCE ascending.
        Assert.Equal(2, chapters[0].Id);
        Assert.Equal("Chapter Two", chapters[0].Title);
        Assert.Equal(string.Empty, chapters[0].Summary);
        Assert.Equal(1, chapters[0].DisplaySequence);

        Assert.Equal(1, chapters[1].Id);
        Assert.Equal("Chapter One", chapters[1].Title);
        Assert.Equal("Summary one", chapters[1].Summary);
        Assert.Equal(string.Empty, chapters[1].Body);
        Assert.Equal(2, chapters[1].DisplaySequence);
        Assert.Equal(new DateTime(2020, 1, 15), chapters[1].CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_materializes_detail_row_and_falls_back_summary_from_body()
    {
        SeedDetailRow(7, "  Detail Title  ", null, "Full body text for excerpt.", 3);

        var chapter = await repository.GetByIdAsync(7);

        Assert.NotNull(chapter);
        Assert.Equal(7, chapter.Id);
        Assert.Equal("Detail Title", chapter.Title);
        Assert.Equal("Full body text for excerpt.", chapter.Body);
        Assert.Equal(3, chapter.DisplaySequence);
        Assert.Equal(DateTime.MinValue, chapter.CreatedAt);
        Assert.False(string.IsNullOrWhiteSpace(chapter.Summary));
        Assert.Contains("Full body", chapter.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_chapter_is_missing()
    {
        var chapter = await repository.GetByIdAsync(999);

        Assert.Null(chapter);
    }

    [Fact]
    public async Task GetByIdAsync_uses_explicit_summary_when_present()
    {
        SeedDetailRow(3, "Titled", "  Explicit summary  ", "Body ignored for summary", 1);

        var chapter = await repository.GetByIdAsync(3);

        Assert.NotNull(chapter);
        Assert.Equal("Explicit summary", chapter.Summary);
        Assert.Equal("Body ignored for summary", chapter.Body);
    }

    [Fact]
    public async Task GetAdjacentChaptersAsync_returns_previous_and_next_in_display_sequence()
    {
        SeedListRow(10, "First", 1, "A", "2020-01-01");
        SeedListRow(20, "Second", 2, "B", "2020-01-02");
        SeedListRow(30, "Third", 3, "C", "2020-01-03");

        var middle = await repository.GetAdjacentChaptersAsync(20);
        Assert.Equal(10, middle.Previous?.Id);
        Assert.Equal(30, middle.Next?.Id);

        var first = await repository.GetAdjacentChaptersAsync(10);
        Assert.Null(first.Previous);
        Assert.Equal(20, first.Next?.Id);

        var last = await repository.GetAdjacentChaptersAsync(30);
        Assert.Equal(20, last.Previous?.Id);
        Assert.Null(last.Next);

        var missing = await repository.GetAdjacentChaptersAsync(999);
        Assert.Null(missing.Previous);
        Assert.Null(missing.Next);
    }

    private void SeedListRow(int id, string title, int displaySequence, string? summary, string createDate)
    {
        dbContext.Database.ExecuteSql(
            $"""
            INSERT INTO BioList (BIOTITLE, Q_BIO_ID, CREATE_DATE, TITLE, DISPLAY_SEQUENCE, SUMMARY)
            VALUES ({title}, {id}, {createDate}, {title}, {displaySequence}, {summary});
            """);
    }

    private void SeedDetailRow(int id, string title, string? summary, string body, int displaySequence)
    {
        dbContext.Database.ExecuteSql(
            $"""
            INSERT INTO BioDetail (Q_BIO_ID, TITLE, SUMMARY, BIO_TEXT, DISPLAY_SEQUENCE)
            VALUES ({id}, {title}, {summary}, {body}, {displaySequence});
            """);
    }
}
