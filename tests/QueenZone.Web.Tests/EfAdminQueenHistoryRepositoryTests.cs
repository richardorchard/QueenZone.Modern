using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfAdminQueenHistoryRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfAdminQueenHistoryRepository repository;

    public EfAdminQueenHistoryRepositoryTests()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        repository = new EfAdminQueenHistoryRepository(dbContext);
    }

    [Fact]
    public async Task CreateUpdateDelete_and_page()
    {
        var firstId = await repository.CreateAsync(Draft("Alpha event", importance: 10));
        var secondId = await repository.CreateAsync(Draft("Beta event", importance: 90));

        var created = await repository.GetByIdAsync(firstId);
        Assert.NotNull(created);
        Assert.Equal("Alpha event", created.Title);
        Assert.Equal(QueenHistoryEventSourceType.Curated, created.SourceType);
        Assert.StartsWith("curated:", created.SourceKey, StringComparison.Ordinal);

        await repository.UpdateAsync(firstId, Draft("Alpha updated", importance: 20));
        var updated = await repository.GetByIdAsync(firstId);
        Assert.NotNull(updated);
        Assert.Equal("Alpha updated", updated.Title);
        Assert.Equal(created.SourceKey, updated.SourceKey);
        Assert.Equal(QueenHistoryEventSourceType.Curated, updated.SourceType);

        var page = await repository.GetPageAsync(new AdminQueenHistoryListFilter(null, null), 1, 50);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(secondId, page.Items[0].Id);
        Assert.Equal(firstId, page.Items[1].Id);

        await repository.DeleteAsync(secondId);
        Assert.Null(await repository.GetByIdAsync(secondId));
        Assert.NotNull(await repository.GetByIdAsync(firstId));
    }

    [Fact]
    public async Task CreateAsync_assigns_unique_source_keys()
    {
        var firstId = await repository.CreateAsync(Draft("First curated"));
        var secondId = await repository.CreateAsync(Draft("Second curated"));

        var first = await repository.GetByIdAsync(firstId);
        var second = await repository.GetByIdAsync(secondId);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.StartsWith("curated:", first.SourceKey, StringComparison.Ordinal);
        Assert.StartsWith("curated:", second.SourceKey, StringComparison.Ordinal);
        Assert.NotEqual(first.SourceKey, second.SourceKey);
    }

    [Fact]
    public async Task Stale_row_version_throws_optimistic_concurrency()
    {
        var id = await repository.CreateAsync(Draft("Concurrency event"));
        var created = await repository.GetByIdAsync(id);
        Assert.NotNull(created?.RowVersion);

        await repository.UpdateAsync(id, Draft("First writer"), created!.RowVersion);

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.UpdateAsync(id, Draft("Second writer"), created.RowVersion));

        var current = await repository.GetByIdAsync(id);
        Assert.Equal("First writer", current!.Title);
    }

    [Fact]
    public async Task SetPublishedAsync_toggles_and_rejects_stale_row_version()
    {
        var id = await repository.CreateAsync(Draft("Publish toggle", importance: 5));
        var created = await repository.GetByIdAsync(id);
        Assert.True(created!.IsPublished);

        await repository.SetPublishedAsync(id, false, created.RowVersion);
        var unpublished = await repository.GetByIdAsync(id);
        Assert.False(unpublished!.IsPublished);

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            repository.SetPublishedAsync(id, true, created.RowVersion));
        Assert.False((await repository.GetByIdAsync(id))!.IsPublished);
    }

    [Fact]
    public async Task Concurrent_contexts_surface_save_changes_concurrency()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var secondContext = new QueenZoneDbContext(options);
        var second = new EfAdminQueenHistoryRepository(secondContext);

        var id = await repository.CreateAsync(Draft("Shared row"));
        var firstLoaded = await repository.GetByIdAsync(id);
        var secondLoaded = await second.GetByIdAsync(id);
        Assert.NotNull(firstLoaded?.RowVersion);
        Assert.NotNull(secondLoaded?.RowVersion);

        await repository.UpdateAsync(id, Draft("First context"), firstLoaded!.RowVersion);

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            second.UpdateAsync(id, Draft("Second context"), secondLoaded!.RowVersion));
        Assert.Equal("First context", (await repository.GetByIdAsync(id))!.Title);
    }

    [Fact]
    public async Task Missing_update_and_delete_throw()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateAsync(9999, Draft("Missing")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteAsync(9999));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SetPublishedAsync(9999, true));
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private static AdminQueenHistoryDraft Draft(string title, int importance = 50) =>
        new(
            title,
            "Summary",
            new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Other,
            importance,
            null,
            true);
}
