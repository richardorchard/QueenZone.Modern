using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryQuoteRepositoryWriteTests
{
    [Fact]
    public async Task CreateAndUpdate_persist_quote_fields()
    {
        var store = new SharedQuoteStore();
        var repository = new InMemoryQuoteRepository(store);

        var id = await repository.CreateAsync(new AdminQuoteDraft("Original", "Speaker A", false));

        var created = await repository.GetByIdAsync(id);
        Assert.NotNull(created);
        Assert.Equal("Original", created.Text);
        Assert.Equal("Speaker A", created.WhoSaid);
        Assert.False(created.IsPublished);

        await repository.UpdateAsync(id, new AdminQuoteDraft("Updated", "Speaker B", true));

        var updated = await repository.GetByIdAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Text);
        Assert.Equal("Speaker B", updated.WhoSaid);
        Assert.True(updated.IsPublished);
    }

    [Fact]
    public async Task Update_missing_quote_throws()
    {
        var repository = new InMemoryQuoteRepository(new SharedQuoteStore());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(42, new AdminQuoteDraft("x", "y", true)));
    }

    [Fact]
    public async Task Delete_removes_quote_and_missing_id_throws()
    {
        var repository = new InMemoryQuoteRepository(new SharedQuoteStore());
        var id = await repository.CreateAsync(new AdminQuoteDraft("Text", "Speaker", true));

        await repository.DeleteAsync(id);

        Assert.Null(await repository.GetByIdAsync(id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(id));
    }

    [Fact]
    public async Task GetRandomPublishedAsync_only_returns_published_quotes()
    {
        var repository = new InMemoryQuoteRepository(new SharedQuoteStore());
        await repository.CreateAsync(new AdminQuoteDraft("Hidden", "Speaker A", false));
        var publishedId = await repository.CreateAsync(new AdminQuoteDraft("Visible", "Speaker B", true));

        var quote = await repository.GetRandomPublishedAsync();

        Assert.NotNull(quote);
        Assert.Equal(publishedId, quote.Id);
    }
}
