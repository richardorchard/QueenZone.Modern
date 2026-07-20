using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfArticleSubmissionRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfArticleSubmissionRepository repository;
    private readonly Guid memberId = Guid.NewGuid();

    public EfArticleSubmissionRepositoryTests()
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
            Email = "article-author@example.com",
            NormalizedEmail = "ARTICLE-AUTHOR@EXAMPLE.COM",
            DisplayName = "Test Author",
            CreatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();

        repository = new EfArticleSubmissionRepository(dbContext);
    }

    [Fact]
    public async Task UpsertDraftAsync_CreatesDraftWithSlug()
    {
        var saved = await repository.UpsertDraftAsync(Draft(null, "My First Article", "Hello world body here."));

        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal("My First Article", saved.Title);
        Assert.Equal(ArticleSubmissionStatus.Draft, saved.Status);
        Assert.Equal("my-first-article", saved.Slug);
    }

    [Fact]
    public async Task UpsertDraftAsync_UpdatesExistingDraft()
    {
        var created = await repository.UpsertDraftAsync(Draft(null, "Original Title", "Body here."));

        var updated = await repository.UpsertDraftAsync(Draft(created.Id, "Updated Title", "Updated body."));

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("updated-title", updated.Slug);
    }

    [Fact]
    public async Task SubmitForReviewAsync_RejectsBodyUnderMinimumLength()
    {
        var draft = await repository.UpsertDraftAsync(Draft(null, "Short", "Too short."));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.SubmitForReviewAsync(draft.Id, memberId));
    }

    [Fact]
    public async Task SubmitForReviewAsync_TransitionsDraftToSubmitted()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);
        var draft = await repository.UpsertDraftAsync(Draft(null, "Long article", body));

        var submitted = await repository.SubmitForReviewAsync(draft.Id, memberId);

        Assert.NotNull(submitted);
        Assert.Equal(ArticleSubmissionStatus.Submitted, submitted!.Status);
        Assert.NotNull(submitted.SubmittedAt);
    }

    [Fact]
    public async Task SubmitForReviewAsync_ReturnsNull_WhenIdNotFound()
    {
        var result = await repository.SubmitForReviewAsync(Guid.NewGuid(), memberId);
        Assert.Null(result);
    }

    [Fact]
    public async Task SubmitForReviewAsync_ReturnsNull_WhenWrongMember()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);
        var draft = await repository.UpsertDraftAsync(Draft(null, "Article", body));

        var result = await repository.SubmitForReviewAsync(draft.Id, Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsOnlyReviewableStatuses()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);

        var draft = await repository.UpsertDraftAsync(Draft(null, "Draft article", body));
        var submitted = await repository.UpsertDraftAsync(Draft(null, "Submitted article", body));
        await repository.SubmitForReviewAsync(submitted.Id, memberId);

        var pending = await repository.GetPendingAsync(1, 50);

        Assert.Single(pending);
        Assert.Equal("Submitted article", pending[0].Title);
        Assert.DoesNotContain(pending, p => p.Id == draft.Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_SetsPublishedAt_WhenPublished()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);
        var draft = await repository.UpsertDraftAsync(Draft(null, "Publish me", body));
        await repository.SubmitForReviewAsync(draft.Id, memberId);

        var published = await repository.UpdateStatusAsync(
            draft.Id,
            ArticleSubmissionStatus.Published,
            "editor@test.local",
            "Looks good",
            null);

        Assert.NotNull(published);
        Assert.Equal(ArticleSubmissionStatus.Published, published!.Status);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task GetPublishedAsync_ReturnsOnlyPublishedArticles()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);
        var article = await repository.UpsertDraftAsync(Draft(null, "Published article", body));
        await repository.SubmitForReviewAsync(article.Id, memberId);
        await repository.UpdateStatusAsync(article.Id, ArticleSubmissionStatus.Published, "ed@test.local", null, null);

        var draft = await repository.UpsertDraftAsync(Draft(null, "Unpublished draft", body));

        var published = await repository.GetPublishedAsync();

        Assert.Single(published);
        Assert.Equal("Published article", published[0].Title);
        Assert.DoesNotContain(published, p => p.Id == draft.Id);
    }

    [Fact]
    public async Task GetDraftsForMemberAsync_ReturnsAllStatusesForMember()
    {
        var body = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars);
        await repository.UpsertDraftAsync(Draft(null, "My Draft", "Short draft."));
        var submitted = await repository.UpsertDraftAsync(Draft(null, "My Submitted", body));
        await repository.SubmitForReviewAsync(submitted.Id, memberId);

        var all = await repository.GetDraftsForMemberAsync(memberId);

        Assert.Equal(2, all.Count);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private ArticleSubmissionDraft Draft(Guid? id, string title, string body) =>
        new(id, memberId, title, null, body, null, null);
}
