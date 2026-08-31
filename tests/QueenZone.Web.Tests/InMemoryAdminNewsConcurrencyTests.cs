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
}
