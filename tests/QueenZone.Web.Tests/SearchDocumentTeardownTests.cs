using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class SearchDocumentTeardownTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly QueenZoneDbContext dbContext;

    public SearchDocumentTeardownTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        AdminNewsSqliteTestHarness.EnsureNewsTable(dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task DeleteBySourceKeysAsync_RemovesSearchDocument_WithTheSourceNewsRow()
    {
        AdminNewsSqliteTestHarness.SeedArticle(dbContext, 900042, "Probe write leftover title");

        var sourceKey = SearchDocumentSourceKey.ForNews(900042);
        dbContext.SearchDocuments.Add(Document(sourceKey, "news", "Probe write leftover title"));
        dbContext.SearchDocuments.Add(Document("news:900043", "news", "Unrelated published news"));
        await dbContext.SaveChangesAsync();

        await dbContext.NewsRows.Where(row => row.NewsId == 900042).ExecuteDeleteAsync();
        await SearchDocumentTeardown.DeleteBySourceKeysAsync(dbContext, [sourceKey]);

        Assert.False(await dbContext.NewsRows.AnyAsync(row => row.NewsId == 900042));
        Assert.False(await dbContext.SearchDocuments.AnyAsync(document => document.SourceKey == sourceKey));
        Assert.True(await dbContext.SearchDocuments.AnyAsync(document => document.SourceKey == "news:900043"));
    }

    [Fact]
    public async Task DeleteBySourceKeysAsync_RemovesArticleAndForumCopies_BySourceKey()
    {
        var articleKey = SearchDocumentSourceKey.ForArticle("probe-article-slug");
        var forumKey = SearchDocumentSourceKey.ForForumThread(4521);
        dbContext.SearchDocuments.Add(Document(articleKey, "article", "Published probe article"));
        dbContext.SearchDocuments.Add(Document(forumKey, "forum", "Forum Write Probe thread"));
        await dbContext.SaveChangesAsync();

        await SearchDocumentTeardown.DeleteBySourceKeysAsync(dbContext, [articleKey, forumKey]);

        Assert.Empty(await dbContext.SearchDocuments.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task DeleteBySourceKeysAsync_IgnoresBlankKeys()
    {
        dbContext.SearchDocuments.Add(Document("news:1", "news", "Keep"));
        await dbContext.SaveChangesAsync();

        await SearchDocumentTeardown.DeleteBySourceKeysAsync(dbContext, ["", "  ", null!]);

        Assert.Equal("news:1", (await dbContext.SearchDocuments.AsNoTracking().SingleAsync()).SourceKey);
    }

    private static SearchDocumentEntity Document(string sourceKey, string contentType, string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            SourceKey = sourceKey,
            ContentType = contentType,
            Title = title,
            Body = title,
            Url = $"/{sourceKey}",
            IndexedAt = DateTimeOffset.UtcNow,
        };
}
