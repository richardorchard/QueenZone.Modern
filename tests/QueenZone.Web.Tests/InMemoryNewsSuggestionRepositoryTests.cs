using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryNewsSuggestionRepositoryTests
{
    [Fact]
    public async Task CreateAsync_GeneratesId_WhenMissing()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        var created = await repository.CreateAsync(NewSuggestion(Guid.Empty));

        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNull_WhenMissing()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        Assert.Null(await repository.UpdateStatusAsync(
            Guid.NewGuid(),
            NewsSuggestionStatus.Rejected,
            "admin@test.local",
            null));
    }

    [Fact]
    public async Task PromoteAndMarkDuplicate_ReturnNull_WhenMissing()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        Assert.Null(await repository.PromoteAsync(Guid.NewGuid(), 1, "admin@test.local", null));
        Assert.Null(await repository.MarkDuplicateAsync(Guid.NewGuid(), 1, "admin@test.local", null));
    }

    [Fact]
    public async Task GetPendingAsync_ClampsPageAndPageSize()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        await repository.CreateAsync(NewSuggestion(Guid.NewGuid()));

        var page = await repository.GetPendingAsync(0, 1000);
        Assert.Single(page);
    }

    private static NewsSuggestion NewSuggestion(Guid id) =>
        new(
            id,
            Guid.NewGuid(),
            "https://example.com/story",
            NewsCandidateDedupe.ComputeUrlHash("https://example.com/story"),
            "Title",
            "Notes",
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
}
