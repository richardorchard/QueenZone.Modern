using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class TriviaFactCsvImporterTests
{
    [Fact]
    public void ReadRows_parses_optional_columns()
    {
        var csvPath = WriteCsv("""
            Text,Category,Difficulty,Source
            A fact with everything,Brian May,easy,"As It Began, p.16"
            A fact with only text,,,
            """);

        var rows = TriviaFactCsvImporter.ReadRows(csvPath);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Brian May", rows[0].Category);
        Assert.Equal("easy", rows[0].Difficulty);
        Assert.Equal("As It Began, p.16", rows[0].Source);
        Assert.Null(rows[1].Category);
        Assert.Null(rows[1].Difficulty);
        Assert.Null(rows[1].Source);
    }

    [Fact]
    public async Task ImportAsync_upserts_by_text()
    {
        var csvPath = WriteCsv("""
            Text,Category,Difficulty,Source
            Existing fact text,Brian May,medium,"As It Began, p.16"
            A brand new fact,Roger Taylor,,
            """);
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        var importedAt = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);

        await using (var setupContext = new QueenZoneDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var importer = new TriviaFactCsvImporter(setupContext);
            await importer.ImportAsync(
                WriteCsv("""
                    Text,Category,Difficulty,Source
                    Existing fact text,Brian May,easy,
                    """),
                new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        await using (var importContext = new QueenZoneDbContext(options))
        {
            var importer = new TriviaFactCsvImporter(importContext);
            var result = await importer.ImportAsync(csvPath, importedAt);

            Assert.Equal(new TriviaFactCsvImportResult(2, 1, 1, 0), result);
        }

        await using var assertContext = new QueenZoneDbContext(options);
        var facts = await assertContext.TriviaFacts
            .OrderBy(fact => fact.Text)
            .ToListAsync();

        Assert.Equal(2, facts.Count);
        Assert.Equal("A brand new fact", facts[0].Text);
        Assert.Equal("Roger Taylor", facts[0].Category);
        Assert.Equal("Existing fact text", facts[1].Text);
        Assert.Equal("medium", facts[1].Difficulty);
        Assert.Equal("As It Began, p.16", facts[1].Source);
        Assert.True(facts[1].IsPublished);
    }

    [Fact]
    public void ReadRows_rejects_unsupported_difficulty()
    {
        var csvPath = WriteCsv("""
            Text,Category,Difficulty,Source
            A fact,Brian May,extreme,
            """);

        Assert.Throws<InvalidOperationException>(() => TriviaFactCsvImporter.ReadRows(csvPath));
    }

    [Fact]
    public void ReadRows_rejects_duplicate_text_within_the_same_csv()
    {
        var csvPath = WriteCsv("""
            Text,Category,Difficulty,Source
            Duplicate fact text,Brian May,,
            Duplicate fact text,Roger Taylor,,
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => TriviaFactCsvImporter.ReadRows(csvPath));
        Assert.Contains("same Text", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAsync_does_not_republish_a_fact_an_admin_has_unpublished()
    {
        var csvPath = WriteCsv("""
            Text,Category,Difficulty,Source
            Original fact text,Brian May,easy,
            """);
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new QueenZoneDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var importer = new TriviaFactCsvImporter(setupContext);
            await importer.ImportAsync(csvPath, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

            var imported = await setupContext.TriviaFacts.SingleAsync(fact => fact.Text == "Original fact text");
            imported.IsPublished = false;
            await setupContext.SaveChangesAsync();
        }

        await using (var reimportContext = new QueenZoneDbContext(options))
        {
            var importer = new TriviaFactCsvImporter(reimportContext);
            var result = await importer.ImportAsync(csvPath, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

            Assert.Equal(new TriviaFactCsvImportResult(1, 0, 0, 1), result);
        }

        await using var assertContext = new QueenZoneDbContext(options);
        var fact = await assertContext.TriviaFacts.SingleAsync(fact => fact.Text == "Original fact text");
        Assert.False(fact.IsPublished);
    }

    private static string WriteCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
        return path;
    }
}
