using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryAdminNewsConcurrencyTests
{
    [Fact]
    public async Task UpdateAsync_stale_updated_at_does_not_overwrite()
    {
        var store = new SharedNewsStore();
        var repository = new InMemoryAdminNewsRepository(store);
        var id = await repository.CreateDraftAsync(
            new AdminNewsDraft(
                "Original title",
                "original-title",
                "Excerpt",
                "Body",
                DateTime.UtcNow,
                null),
            "a@test.local");
        var created = await repository.GetByIdAsync(id);
        Assert.NotNull(created);

        await repository.UpdateAsync(
            id,
            new AdminNewsDraft(
                "First writer",
                "first-writer",
                "Excerpt",
                "Body",
                DateTime.UtcNow,
                null),
            "a@test.local",
            created!.UpdatedAt);

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.UpdateAsync(
                id,
                new AdminNewsDraft(
                    "Stale overwrite",
                    "stale-overwrite",
                    "Excerpt",
                    "Body",
                    DateTime.UtcNow,
                    null),
                "b@test.local",
                created.UpdatedAt));

        var current = await repository.GetByIdAsync(id);
        Assert.Equal("First writer", current!.Title);
    }

    [Fact]
    public async Task Publish_unpublish_and_delete_reject_stale_updated_at()
    {
        var store = new SharedNewsStore();
        var repository = new InMemoryAdminNewsRepository(store);
        var id = await repository.CreateDraftAsync(
            new AdminNewsDraft("Lifecycle", "lifecycle", "Excerpt", "Body", DateTime.UtcNow.Date, null),
            "a@test.local");
        var created = await repository.GetByIdAsync(id);

        await repository.UpdateAsync(
            id,
            new AdminNewsDraft("Lifecycle 2", "lifecycle-2", "Excerpt", "Body", DateTime.UtcNow.Date, null),
            "a@test.local");

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.PublishAsync(id, "a@test.local", created!.UpdatedAt));
        var current = await repository.GetByIdAsync(id);
        await repository.PublishAsync(id, "a@test.local", current!.UpdatedAt);
        current = await repository.GetByIdAsync(id);
        Assert.NotNull(current);
        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.UnpublishAsync(id, "a@test.local", created.UpdatedAt));
        await repository.UnpublishAsync(id, "a@test.local", current.UpdatedAt);
        current = await repository.GetByIdAsync(id);
        Assert.NotNull(current);
        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.DeleteAsync(id, "a@test.local", created.UpdatedAt));
        await repository.DeleteAsync(id, "a@test.local", current.UpdatedAt);
        Assert.Null(await repository.GetByIdAsync(id));
    }

    [Fact]
    public void Concurrency_helpers_distinguish_stale_from_missing()
    {
        Assert.Equal("custom", new OptimisticConcurrencyException("custom").Message);
        QueenZoneConcurrency.EnsureUpdated(1, exists: false, "unused");
        Assert.Throws<OptimisticConcurrencyException>(() =>
            QueenZoneConcurrency.EnsureUpdated(0, exists: true, "missing"));
        Assert.Throws<InvalidOperationException>(() =>
            QueenZoneConcurrency.EnsureUpdated(0, exists: false, "not found"));
        Assert.False(QueenZoneConcurrency.RowVersionEquals(null, [1]));
        Assert.False(QueenZoneConcurrency.RowVersionEquals([1], null));
        Assert.True(QueenZoneConcurrency.RowVersionEquals([1, 2], [1, 2]));
    }

    [Fact]
    public void AdminNewsForm_parses_roundtrip_updated_at()
    {
        var stamp = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);
        var form = new QueenZone.Web.Pages.Admin.News.AdminNewsForm
        {
            ExpectedUpdatedAt = stamp.ToString("o"),
        };
        Assert.Equal(stamp, form.ParseExpectedUpdatedAt());
        Assert.Null(new QueenZone.Web.Pages.Admin.News.AdminNewsForm().ParseExpectedUpdatedAt());
    }
}
