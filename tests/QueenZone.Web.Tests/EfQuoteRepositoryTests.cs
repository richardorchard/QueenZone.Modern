using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfQuoteRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfQuoteRepository repository;

    public EfQuoteRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS QUEEN_QUOTE_T (
                QUEEN_QUOTE_ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                QUEEN_QUOTE TEXT NULL,
                WHO_SAID TEXT NULL,
                Q_MEMBER_ID INTEGER NOT NULL DEFAULT 0,
                USER_ID INTEGER NOT NULL DEFAULT 0,
                CREATE_DATE TEXT NOT NULL,
                DISPLAY INTEGER NOT NULL DEFAULT 0
            );
            """);
        repository = new EfQuoteRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_persists_a_published_quote_immediately()
    {
        var id = await repository.CreateAsync(new AdminQuoteDraft("A kind of magic.", "Freddie Mercury", true));

        var quote = await repository.GetByIdAsync(id);

        Assert.NotNull(quote);
        Assert.Equal("A kind of magic.", quote.Text);
        Assert.Equal("Freddie Mercury", quote.WhoSaid);
        Assert.True(quote.IsPublished);
    }

    [Fact]
    public async Task GetAllAsync_returns_every_quote_regardless_of_publish_state()
    {
        await repository.CreateAsync(new AdminQuoteDraft("Quote one", "Speaker A", true));
        await repository.CreateAsync(new AdminQuoteDraft("Quote two", "Speaker B", false));

        var quotes = await repository.GetAllAsync();

        Assert.Equal(2, quotes.Count);
    }

    [Fact]
    public async Task GetRandomPublishedAsync_only_returns_published_quotes()
    {
        await repository.CreateAsync(new AdminQuoteDraft("Hidden quote", "Speaker A", false));
        var publishedId = await repository.CreateAsync(new AdminQuoteDraft("Visible quote", "Speaker B", true));

        var quote = await repository.GetRandomPublishedAsync();

        Assert.NotNull(quote);
        Assert.Equal(publishedId, quote.Id);
    }

    [Fact]
    public async Task GetRandomPublishedAsync_returns_null_when_nothing_is_published()
    {
        await repository.CreateAsync(new AdminQuoteDraft("Hidden quote", "Speaker A", false));

        var quote = await repository.GetRandomPublishedAsync();

        Assert.Null(quote);
    }

    [Fact]
    public async Task UpdateAsync_overwrites_text_speaker_and_publish_state()
    {
        var id = await repository.CreateAsync(new AdminQuoteDraft("Original", "Speaker A", false));

        await repository.UpdateAsync(id, new AdminQuoteDraft("Updated", "Speaker B", true));

        var quote = await repository.GetByIdAsync(id);
        Assert.NotNull(quote);
        Assert.Equal("Updated", quote.Text);
        Assert.Equal("Speaker B", quote.WhoSaid);
        Assert.True(quote.IsPublished);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_quote_is_missing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(9999, new AdminQuoteDraft("Text", "Speaker", true)));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_quote()
    {
        var id = await repository.CreateAsync(new AdminQuoteDraft("Text", "Speaker", true));

        await repository.DeleteAsync(id);

        Assert.Null(await repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_throws_when_quote_is_missing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(9999));
    }

    [Fact]
    public async Task SetPublishedAsync_toggles_publish_state()
    {
        var id = await repository.CreateAsync(new AdminQuoteDraft("Text", "Speaker", true));

        await repository.SetPublishedAsync(id, false);

        var quote = await repository.GetByIdAsync(id);
        Assert.NotNull(quote);
        Assert.False(quote.IsPublished);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
