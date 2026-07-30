using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryBiographyRepositoryWriteTests
{
    [Fact]
    public async Task CreateAndUpdate_persist_chapter_fields()
    {
        var store = new SharedBiographyStore();
        var repository = new InMemoryBiographyRepository(store);

        var id = await repository.CreateAsync(
            new AdminBiographyDraft("1970", "Early years", "<p>Original</p>", 2));

        var created = await repository.GetByIdAsync(id);
        Assert.NotNull(created);
        Assert.Equal("1970", created.Title);
        Assert.Equal("Early years", created.Summary);
        Assert.Equal("<p>Original</p>", created.Body);
        Assert.Equal(2, created.DisplaySequence);

        await repository.UpdateAsync(
            id,
            new AdminBiographyDraft("1970 updated", "Revised", "<p>Updated</p>", 4));

        var updated = await repository.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("1970 updated", updated.Title);
        Assert.Equal("Revised", updated.Summary);
        Assert.Equal("<p>Updated</p>", updated.Body);
        Assert.Equal(4, updated.DisplaySequence);
    }

    [Fact]
    public async Task Update_missing_chapter_throws()
    {
        var repository = new InMemoryBiographyRepository(new SharedBiographyStore());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(42, new AdminBiographyDraft("x", "", "<p>y</p>", 1)));
    }
}
