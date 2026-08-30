using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfTriviaRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfTriviaRepository repository;

    public EfTriviaRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        repository = new EfTriviaRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_persists_a_published_fact_immediately()
    {
        var id = await repository.CreateAsync(
            new AdminTriviaDraft("Freddie was born in Zanzibar.", true, "Band", TriviaDifficulty.Easy, "Bio"));

        var fact = await repository.GetByIdAsync(id);

        Assert.NotNull(fact);
        Assert.Equal("Freddie was born in Zanzibar.", fact.Text);
        Assert.Equal("Band", fact.Category);
        Assert.Equal(TriviaDifficulty.Easy, fact.Difficulty);
        Assert.Equal("Bio", fact.Source);
        Assert.True(fact.IsPublished);
    }

    [Fact]
    public async Task GetAllAsync_returns_every_fact_regardless_of_publish_state()
    {
        await repository.CreateAsync(new AdminTriviaDraft("Fact one", true, "Band"));
        await repository.CreateAsync(new AdminTriviaDraft("Fact two", false, "Albums"));

        var facts = await repository.GetAllAsync();

        Assert.Equal(2, facts.Count);
    }

    [Fact]
    public async Task GetRandomPublishedAsync_only_returns_published_facts()
    {
        await repository.CreateAsync(new AdminTriviaDraft("Hidden fact", false, "Band"));
        var publishedId = await repository.CreateAsync(new AdminTriviaDraft("Visible fact", true, "Albums"));

        var fact = await repository.GetRandomPublishedAsync();

        Assert.NotNull(fact);
        Assert.Equal(publishedId, fact.Id);
        Assert.True(fact.IsPublished);
    }

    [Fact]
    public async Task GetRandomPublishedAsync_returns_null_when_nothing_is_published()
    {
        await repository.CreateAsync(new AdminTriviaDraft("Hidden fact", false));

        var fact = await repository.GetRandomPublishedAsync();

        Assert.Null(fact);
    }

    [Fact]
    public async Task UpdateAsync_overwrites_text_category_and_publish_state()
    {
        var id = await repository.CreateAsync(
            new AdminTriviaDraft("Original", false, "Band", TriviaDifficulty.Easy, "Note"));

        await repository.UpdateAsync(
            id,
            new AdminTriviaDraft("Updated", true, "Albums", TriviaDifficulty.Hard, "Revised"));

        var fact = await repository.GetByIdAsync(id);
        Assert.NotNull(fact);
        Assert.Equal("Updated", fact.Text);
        Assert.Equal("Albums", fact.Category);
        Assert.Equal(TriviaDifficulty.Hard, fact.Difficulty);
        Assert.Equal("Revised", fact.Source);
        Assert.True(fact.IsPublished);
    }

    [Fact]
    public async Task UpdateAsync_throws_when_fact_is_missing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(9999, new AdminTriviaDraft("Text", true)));
    }

    [Fact]
    public async Task DeleteAsync_removes_the_fact()
    {
        var id = await repository.CreateAsync(new AdminTriviaDraft("Text", true));

        await repository.DeleteAsync(id);

        Assert.Null(await repository.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_throws_when_fact_is_missing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(9999));
    }

    [Fact]
    public async Task SetPublishedAsync_toggles_publish_state()
    {
        var id = await repository.CreateAsync(new AdminTriviaDraft("Text", true));

        await repository.SetPublishedAsync(id, false);

        var fact = await repository.GetByIdAsync(id);
        Assert.NotNull(fact);
        Assert.False(fact.IsPublished);
        Assert.Null(await repository.GetRandomPublishedAsync());
    }

    [Fact]
    public async Task SetPublishedAsync_throws_when_fact_is_missing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SetPublishedAsync(9999, true));
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
