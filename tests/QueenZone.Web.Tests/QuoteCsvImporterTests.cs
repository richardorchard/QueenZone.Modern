using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class QuoteCsvImporterTests
{
    [Fact]
    public void ReadRows_parses_context_and_source_identity()
    {
        var csvPath = WriteCsv("""
            Text,WhoSaid,Context,SourceType,SourceKey
            A quote with context,Brian May,On discovering Buddy Holly,AsItBeganBook,a-quote-with-context-brian-may
            A quote without context,Freddie Mercury,,AsItBeganBook,a-quote-without-context-freddie-mercury
            """);

        var rows = QuoteCsvImporter.ReadRows(csvPath);

        Assert.Equal(2, rows.Count);
        Assert.Equal("On discovering Buddy Holly", rows[0].Context);
        Assert.Null(rows[1].Context);
        Assert.All(rows, row => Assert.Equal(QuoteSourceType.AsItBeganBook, row.SourceType));
    }

    [Fact]
    public async Task ImportAsync_upserts_by_source_type_and_key()
    {
        var csvPath = WriteCsv("""
            Text,WhoSaid,Context,SourceType,SourceKey
            Updated quote text,Brian May,Updated context,AsItBeganBook,existing-quote-brian-may
            A brand new quote,Roger Taylor,,AsItBeganBook,new-quote-roger-taylor
            """);
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        var importedAt = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);

        await using (var setupContext = new QueenZoneDbContext(options))
        {
            await setupContext.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS QUEEN_QUOTE_T (
                    QUEEN_QUOTE_ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    QUEEN_QUOTE TEXT NULL,
                    WHO_SAID TEXT NULL,
                    Q_MEMBER_ID INTEGER NOT NULL DEFAULT 0,
                    USER_ID INTEGER NOT NULL DEFAULT 0,
                    CREATE_DATE TEXT NOT NULL,
                    DISPLAY INTEGER NOT NULL DEFAULT 0,
                    CONTEXT TEXT NULL,
                    SOURCE_TYPE TEXT NULL,
                    SOURCE_KEY TEXT NULL
                );
                """);
            var importer = new QuoteCsvImporter(setupContext);
            await importer.ImportAsync(
                WriteCsv("""
                    Text,WhoSaid,Context,SourceType,SourceKey
                    Original quote text,Brian May,Original context,AsItBeganBook,existing-quote-brian-may
                    """),
                new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        await using (var importContext = new QueenZoneDbContext(options))
        {
            var importer = new QuoteCsvImporter(importContext);
            var result = await importer.ImportAsync(csvPath, importedAt);

            Assert.Equal(new QuoteCsvImportResult(2, 1, 1, 0), result);
        }

        await using var assertContext = new QueenZoneDbContext(options);
        var quotes = await assertContext.Quotes
            .OrderBy(quote => quote.SourceKey)
            .ToListAsync();

        Assert.Equal(2, quotes.Count);
        Assert.Equal("Updated quote text", quotes[0].Text);
        Assert.Equal("Updated context", quotes[0].Context);
        Assert.True(quotes[0].IsPublished);
        Assert.Equal("A brand new quote", quotes[1].Text);
        Assert.Null(quotes[1].Context);
    }

    [Fact]
    public void ReadRows_rejects_unsupported_source_type()
    {
        var csvPath = WriteCsv("""
            Text,WhoSaid,Context,SourceType,SourceKey
            A quote,Someone,,NotARealSource,a-quote-someone
            """);

        Assert.Throws<InvalidOperationException>(() => QuoteCsvImporter.ReadRows(csvPath));
    }

    private static string WriteCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
        return path;
    }
}
