using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class QueenHistoryCsvImporterTests
{
    [Fact]
    public void ReadRows_parses_supported_date_precisions_and_wikipedia_source()
    {
        var csvPath = WriteCsv("""
            Title,Summary,EventDate,DatePrecision,Category,Importance,SourceType,SourceKey,SourceUrl
            Exact event,Exact summary,1985-07-13,ExactDate,Concert,100,Wikipedia,exact-event,https://example.com/exact
            Month event,Month summary,1975-10,MonthYear,Release,80,Wikipedia,month-event,https://example.com/month
            Year event,Year summary,1970,YearOnly,SiteHistory,70,Wikipedia,year-event,https://example.com/year
            """);

        var rows = QueenHistoryCsvImporter.ReadRows(csvPath);

        Assert.Equal(3, rows.Count);
        Assert.Equal(new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc), rows[0].EventDate);
        Assert.Equal(new DateTime(1975, 10, 1, 0, 0, 0, DateTimeKind.Utc), rows[1].EventDate);
        Assert.Equal(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), rows[2].EventDate);
        Assert.All(rows, row => Assert.Equal(QueenHistoryEventSourceType.Wikipedia, row.SourceType));
    }

    [Fact]
    public async Task ImportAsync_upserts_by_source_type_and_key()
    {
        var csvPath = WriteCsv("""
            Title,Summary,EventDate,DatePrecision,Category,Importance,SourceType,SourceKey,SourceUrl
            Freddie Mercury born,Updated summary,1946-09-05,ExactDate,Birthday,95,Wikipedia,freddie-mercury-born-1946-09-05,https://example.com/freddie
            New event,New summary,1985-07-13,ExactDate,Concert,100,Wikipedia,new-event,https://example.com/new
            """);
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        var importedAt = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);

        await using (var setupContext = new QueenZoneDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            setupContext.QueenHistoryEvents.Add(new QueenHistoryEventEntity
            {
                Title = "Freddie Mercury born",
                Summary = "Old summary",
                EventDate = new DateTime(1946, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                DatePrecision = QueenHistoryDatePrecision.ExactDate,
                Category = QueenHistoryEventCategory.Birthday,
                Importance = 90,
                SourceType = QueenHistoryEventSourceType.Wikipedia,
                SourceKey = "freddie-mercury-born-1946-09-05",
                SourceUrl = "https://example.com/old",
                IsPublished = true,
                CreatedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc),
            });
            await setupContext.SaveChangesAsync();
        }

        await using (var importContext = new QueenZoneDbContext(options))
        {
            var importer = new QueenHistoryCsvImporter(importContext);
            var result = await importer.ImportAsync(csvPath, importedAt);

            Assert.Equal(new QueenHistoryCsvImportResult(2, 1, 1, 0), result);
        }

        await using var assertContext = new QueenZoneDbContext(options);
        var events = await assertContext.QueenHistoryEvents
            .OrderBy(item => item.SourceKey)
            .ToListAsync();

        Assert.Equal(2, events.Count);
        Assert.Equal("Updated summary", events[0].Summary);
        Assert.Equal(importedAt, events[0].VerifiedAt);
        Assert.Equal("New event", events[1].Title);
    }

    private static string WriteCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content.ReplaceLineEndings(Environment.NewLine));
        return path;
    }
}
