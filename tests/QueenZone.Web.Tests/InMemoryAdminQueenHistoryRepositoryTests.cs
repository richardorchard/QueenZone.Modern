using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryAdminQueenHistoryRepositoryTests
{
    [Fact]
    public async Task CreateAndUpdate_persist_fields_without_changing_source()
    {
        var store = new SharedQueenHistoryStore();
        var repository = new InMemoryAdminQueenHistoryRepository(store);

        var id = await repository.CreateAsync(Draft("Original", importance: 10, isPublished: false));

        var created = await repository.GetByIdAsync(id);
        Assert.NotNull(created);
        Assert.Equal("Original", created.Title);
        Assert.Equal(QueenHistoryEventSourceType.Curated, created.SourceType);
        Assert.StartsWith("curated:", created.SourceKey, StringComparison.Ordinal);
        Assert.False(created.IsPublished);
        var sourceKey = created.SourceKey;

        await repository.UpdateAsync(
            id,
            Draft("Updated", summary: "Changed summary", importance: 80, isPublished: true));

        var updated = await repository.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Title);
        Assert.Equal("Changed summary", updated.Summary);
        Assert.Equal(80, updated.Importance);
        Assert.True(updated.IsPublished);
        Assert.Equal(QueenHistoryEventSourceType.Curated, updated.SourceType);
        Assert.Equal(sourceKey, updated.SourceKey);
    }

    [Fact]
    public async Task Delete_and_SetPublished_and_missing_ids_throw()
    {
        var repository = new InMemoryAdminQueenHistoryRepository(new SharedQueenHistoryStore());
        var id = await repository.CreateAsync(Draft("To delete"));

        await repository.SetPublishedAsync(id, false);
        var unpublished = await repository.GetByIdAsync(id);
        Assert.NotNull(unpublished);
        Assert.False(unpublished.IsPublished);

        await repository.DeleteAsync(id);
        Assert.Null(await repository.GetByIdAsync(id));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(id, Draft("Missing")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SetPublishedAsync(id, true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(42, Draft("Never existed")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SetPublishedAsync(42, true));
    }

    [Fact]
    public async Task GetPageAsync_pages_and_filters_by_published_and_query()
    {
        var store = new SharedQueenHistoryStore();
        var repository = new InMemoryAdminQueenHistoryRepository(store);
        for (var i = 1; i <= 51; i++)
        {
            await repository.CreateAsync(Draft($"Page item {i:000}", importance: i));
        }

        var page2 = await repository.GetPageAsync(new AdminQueenHistoryListFilter(null, null), 2, 50);
        Assert.Equal(51, page2.TotalCount);
        Assert.Equal(2, page2.Page);
        Assert.Equal(50, page2.PageSize);
        Assert.Single(page2.Items);

        var publishedId = await repository.CreateAsync(Draft("Visible needle event", isPublished: true));
        var hiddenId = await repository.CreateAsync(Draft("Hidden needle event", isPublished: false));
        await repository.CreateAsync(Draft("Other title", summary: "Contains needle in summary", isPublished: true));

        var published = await repository.GetPageAsync(new AdminQueenHistoryListFilter(true, "needle"), 1, 50);
        Assert.Equal(2, published.TotalCount);
        Assert.All(published.Items, item => Assert.True(item.IsPublished));
        Assert.DoesNotContain(published.Items, item => item.Id == hiddenId);

        var unpublished = await repository.GetPageAsync(new AdminQueenHistoryListFilter(false, "needle"), 1, 50);
        Assert.Equal(hiddenId, Assert.Single(unpublished.Items).Id);
        Assert.Contains(published.Items, item => item.Id == publishedId);
    }

    [Fact]
    public async Task GetAllPublished_on_paired_public_repo_sees_published_creates_only()
    {
        var store = new SharedQueenHistoryStore();
        var admin = new InMemoryAdminQueenHistoryRepository(store);
        var publicRepo = new InMemoryQueenHistoryRepository(store);

        var publishedId = await admin.CreateAsync(Draft("Shared published", isPublished: true));
        await admin.CreateAsync(Draft("Shared hidden", isPublished: false));

        var published = await publicRepo.GetAllPublishedAsync();
        Assert.Contains(published, item => item.Id == publishedId && item.Title == "Shared published");
        Assert.DoesNotContain(published, item => item.Title == "Shared hidden");
    }

    private static AdminQueenHistoryDraft Draft(
        string title,
        string summary = "Summary",
        int importance = 50,
        bool isPublished = true) =>
        new(
            title,
            summary,
            new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Other,
            importance,
            null,
            isPublished);
}
