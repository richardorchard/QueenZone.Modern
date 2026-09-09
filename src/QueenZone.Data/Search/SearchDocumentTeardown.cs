using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Deletes <c>SearchDocument</c> copies by <c>SourceKey</c> when probe or RealData teardown
/// removes the source row. Publish and reindex write <c>news:{id}</c> / <c>article:{slug}</c>
/// keys that do not contain the <c>uie2e-</c> marker, so title-only cleanup misses them.
/// </summary>
public static class SearchDocumentTeardown
{
    public static async Task DeleteBySourceKeysAsync(
        QueenZoneDbContext dbContext,
        IEnumerable<string> sourceKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(sourceKeys);

        var keys = sourceKeys
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (keys.Count == 0)
        {
            return;
        }

        await dbContext.SearchDocuments
            .Where(document => keys.Contains(document.SourceKey))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
