using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfNewsSuggestionRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfNewsSuggestionRepository repository;
    private readonly Guid memberId = Guid.NewGuid();

    public EfNewsSuggestionRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();

        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = memberId,
            Email = "news-ef@example.com",
            NormalizedEmail = "NEWS-EF@EXAMPLE.COM",
            DisplayName = "EF News Fan",
            CreatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();

        repository = new EfNewsSuggestionRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_PersistsPendingSuggestion()
    {
        var id = Guid.NewGuid();
        var url = "https://example.com/queen-story";
        var urlHash = NewsCandidateDedupe.ComputeUrlHash(url);

        var created = await repository.CreateAsync(new NewsSuggestion(
            id,
            memberId,
            url,
            urlHash,
            "Headline",
            "Notes",
            NewsSuggestionStatus.Pending,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.Equal(id, created.Id);
        Assert.Equal(NewsSuggestionStatus.Pending, created.Status);

        var loaded = await repository.GetByIdAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("EF News Fan", loaded!.SubmitterDisplayName);
    }

    [Fact]
    public async Task HasActiveDuplicateAsync_DetectsPendingAndUnderReview()
    {
        var urlHash = NewsCandidateDedupe.ComputeUrlHash("https://example.com/dup");
        await repository.CreateAsync(NewSuggestion(urlHash, "https://example.com/dup"));

        Assert.True(await repository.HasActiveDuplicateAsync(urlHash));

        var suggestion = await repository.GetPendingAsync(1, 10);
        var item = Assert.Single(suggestion);
        await repository.UpdateStatusAsync(item.Id, NewsSuggestionStatus.UnderReview, "admin@test.local", null);
        Assert.True(await repository.HasActiveDuplicateAsync(urlHash));

        await repository.UpdateStatusAsync(item.Id, NewsSuggestionStatus.Rejected, "admin@test.local", null);
        Assert.False(await repository.HasActiveDuplicateAsync(urlHash));
    }

    [Fact]
    public async Task PromoteAndMarkDuplicate_PersistLinks()
    {
        var created = await repository.CreateAsync(NewSuggestion(
            NewsCandidateDedupe.ComputeUrlHash("https://example.com/promote"),
            "https://example.com/promote"));

        var promoted = await repository.PromoteAsync(
            created.Id,
            42,
            "admin@test.local",
            "Promoted");
        Assert.Equal(NewsSuggestionStatus.Promoted, promoted!.Status);
        Assert.Equal(42, promoted.PromotedNewsId);

        dbContext.NewsDiscoverySources.Add(new NewsDiscoverySourceEntity
        {
            Key = "test-source",
            DisplayName = "Test",
            HomepageUrl = "https://example.com/",
            SourceType = NewsDiscoverySourceType.Rss,
            TrustTier = NewsDiscoveryTrustTier.Primary,
            PollIntervalMinutes = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        dbContext.NewsCandidates.Add(new NewsCandidateEntity
        {
            SourceId = dbContext.NewsDiscoverySources.Single().Id,
            SourceUrl = "https://example.com/dup-link",
            CanonicalUrl = "https://example.com/dup-link",
            CanonicalUrlHash = NewsCandidateDedupe.ComputeUrlHash("https://example.com/dup-link"),
            SourceTitle = "Dup candidate",
            DiscoveredAt = DateTime.UtcNow,
            Status = NewsCandidateStatus.Discovered,
        });
        await dbContext.SaveChangesAsync();
        var candidateId = dbContext.NewsCandidates.Single().Id;

        var duplicate = await repository.CreateAsync(NewSuggestion(
            NewsCandidateDedupe.ComputeUrlHash("https://example.com/dup-link"),
            "https://example.com/dup-link"));
        var marked = await repository.MarkDuplicateAsync(
            duplicate.Id,
            candidateId,
            "admin@test.local",
            "Already discovered");
        Assert.Equal(NewsSuggestionStatus.Duplicate, marked!.Status);
        Assert.Equal(candidateId, marked.DuplicateCandidateId);
    }

    [Fact]
    public async Task CountBySubmitterSinceAsync_CountsRecentRows()
    {
        for (var i = 0; i < 3; i++)
        {
            await repository.CreateAsync(NewSuggestion(
                NewsCandidateDedupe.ComputeUrlHash($"https://example.com/count-{i}"),
                $"https://example.com/count-{i}"));
        }

        var count = await repository.CountBySubmitterSinceAsync(memberId, DateTimeOffset.UtcNow.AddDays(-1));
        Assert.Equal(3, count);
    }

    private NewsSuggestion NewSuggestion(string urlHash, string url) =>
        new(
            Guid.NewGuid(),
            memberId,
            url,
            urlHash,
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

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
