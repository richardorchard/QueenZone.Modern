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

    [Fact]
    public async Task GetBySubmitterAsync_ReturnsOnlyOwnedRows_NewestFirst_Paginated()
    {
        var repository = new InMemoryNewsSuggestionRepository();
        var memberId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await repository.CreateAsync(NewSuggestion(Guid.NewGuid(), memberId, "https://example.com/a", DateTimeOffset.UtcNow.AddMinutes(-2)));
        await repository.CreateAsync(NewSuggestion(Guid.NewGuid(), memberId, "https://example.com/b", DateTimeOffset.UtcNow.AddMinutes(-1)));
        await repository.CreateAsync(NewSuggestion(Guid.NewGuid(), otherId, "https://example.com/other", DateTimeOffset.UtcNow));

        var page = await repository.GetBySubmitterAsync(memberId, page: 1, pageSize: 1);

        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Contains("example.com/b", page.Items[0].Url, StringComparison.Ordinal);
    }

    private static NewsSuggestion NewSuggestion(Guid id) =>
        NewSuggestion(id, Guid.NewGuid(), "https://example.com/story", DateTimeOffset.UtcNow);

    private static NewsSuggestion NewSuggestion(
        Guid id,
        Guid submitterMemberId,
        string url,
        DateTimeOffset submittedAt) =>
        new(
            id,
            submitterMemberId,
            url,
            NewsCandidateDedupe.ComputeUrlHash(url),
            "Title",
            "Notes",
            NewsSuggestionStatus.Pending,
            submittedAt,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
}
