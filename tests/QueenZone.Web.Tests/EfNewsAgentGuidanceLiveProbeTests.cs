using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe. Sqlite/in-memory providers never configure a retrying
/// execution strategy, so they cannot catch a repository method that calls
/// <c>Database.BeginTransactionAsync</c> directly instead of routing it through
/// <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c> — that combination only throws
/// "does not support user-initiated transactions" against a real SQL Server connection with
/// retry-on-failure enabled, which is exactly what <see cref="EfNewsAgentGuidanceRepository"/>
/// hit in production. Restores the probed type's pre-existing draft/published rows exactly so
/// the mirror's guidance config is left unchanged.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfNewsAgentGuidanceLiveProbeTests
{
    private const string ProbeEditor = "legacy-write-probe@queenzone.local";
    private const NewsAgentGuidanceType ProbeType = NewsAgentGuidanceType.Triage;

    [Fact]
    public async Task Publish_rollback_and_restore_default_run_under_the_configured_execution_strategy_when_enabled()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var probeContent = $"Execution-strategy probe guidance {uniqueSuffix}.";

        await using var provider = CreateProvider(connectionString);

        var (originalPublished, originalDraft) = await CaptureOriginalStateAsync(provider);

        // Every row this probe touches is recorded as soon as it exists, before any assertion
        // that could throw, so a failure partway through (e.g. the very regression this probe
        // guards against) still leaves accurate cleanup instructions in the finally block below
        // rather than surfacing a misleading cleanup-assertion failure in place of the real one.
        var createdRowIds = new List<int>();

        try
        {
            await using var scope = provider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<INewsAgentGuidanceRepository>();

            var draft = await repository.SaveDraftAsync(
                ProbeType,
                probeContent,
                ProbeEditor,
                originalDraft?.RowVersion);
            createdRowIds.Add(draft.Id);
            Assert.Equal(probeContent, draft.Content);

            var published = await repository.PublishDraftAsync(ProbeType, ProbeEditor, draft.RowVersion);
            Assert.Equal(probeContent, published.Content);
            Assert.Null(await repository.GetDraftAsync(ProbeType));

            var rolledBack = await repository.RollbackAsync(ProbeType, published.Id, ProbeEditor);
            createdRowIds.Add(rolledBack.Id);
            Assert.Equal(probeContent, rolledBack.Content);
            Assert.NotEqual(published.Id, rolledBack.Id);

            var restored = await repository.RestoreCompiledDefaultAsync(ProbeType, ProbeEditor);
            createdRowIds.Add(restored.Id);
            Assert.Equal(string.Empty, restored.Content);
            Assert.NotEqual(rolledBack.Id, restored.Id);
        }
        finally
        {
            await RestoreOriginalStateAsync(provider, createdRowIds, originalPublished, originalDraft);
        }

        // Only reached if the probe above completed without throwing, so this never masks a
        // failure raised (and already reported) from inside the try block.
        await using var verifyScope = provider.CreateAsyncScope();
        var verifyRepository = verifyScope.ServiceProvider.GetRequiredService<INewsAgentGuidanceRepository>();
        Assert.Equal(originalPublished?.Content, (await verifyRepository.GetPublishedAsync(ProbeType))?.Content);
        Assert.Equal(originalDraft?.Content, (await verifyRepository.GetDraftAsync(ProbeType))?.Content);
    }

    private static async Task<(NewsAgentGuidanceRevisionEntity? Published, NewsAgentGuidanceRevisionEntity? Draft)> CaptureOriginalStateAsync(
        ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();

        var published = await dbContext.NewsAgentGuidanceRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Type == ProbeType && item.Status == NewsAgentGuidanceStatus.Published);
        var draft = await dbContext.NewsAgentGuidanceRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Type == ProbeType && item.Status == NewsAgentGuidanceStatus.Draft);
        return (published, draft);
    }

    private static async Task RestoreOriginalStateAsync(
        ServiceProvider provider,
        IReadOnlyCollection<int> createdRowIds,
        NewsAgentGuidanceRevisionEntity? originalPublished,
        NewsAgentGuidanceRevisionEntity? originalDraft)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();

        // The probe's SaveDraftAsync call reuses the existing draft row (same Id) when one was
        // already present, so that row must be restored in place rather than deleted.
        var reusedDraftId = originalDraft?.Id;
        var idsToDelete = createdRowIds.Where(id => id != reusedDraftId).ToList();
        if (idsToDelete.Count > 0)
        {
            await dbContext.NewsAgentGuidanceRevisions
                .Where(item => idsToDelete.Contains(item.Id))
                .ExecuteDeleteAsync();
        }

        if (originalDraft is not null)
        {
            await dbContext.NewsAgentGuidanceRevisions
                .Where(item => item.Id == originalDraft.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Content, originalDraft.Content)
                    .SetProperty(item => item.ContentHash, originalDraft.ContentHash)
                    .SetProperty(item => item.Status, NewsAgentGuidanceStatus.Draft)
                    .SetProperty(item => item.CreatedByEmail, originalDraft.CreatedByEmail)
                    .SetProperty(item => item.CreatedAt, originalDraft.CreatedAt)
                    .SetProperty(item => item.PublishedAt, originalDraft.PublishedAt)
                    .SetProperty(item => item.PublishedByEmail, originalDraft.PublishedByEmail));
        }
        if (originalPublished is not null)
        {
            await dbContext.NewsAgentGuidanceRevisions
                .Where(item => item.Id == originalPublished.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, NewsAgentGuidanceStatus.Published));
        }
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddQueenZoneLegacyData(connectionString);
        return services.BuildServiceProvider();
    }

    private static bool IsWriteProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_NEWS_AGENT_GUIDANCE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
