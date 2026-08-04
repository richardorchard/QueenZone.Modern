using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemorySearchIndexService(SharedSearchIndexStore store) : ISearchIndexService
{
    public Task UpsertAsync(SearchDocumentEntity document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.SourceKey);
        store.Upsert(document);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string sourceKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);
        store.Remove(sourceKey);
        return Task.CompletedTask;
    }

    public Task ReplaceContentTypeAsync(
        string contentType,
        IReadOnlyList<SearchDocumentEntity> documents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(documents);
        store.ReplaceContentType(contentType, documents);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, int>> GetContentTypeCountsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, int> counts = store.GetAll()
            .GroupBy(d => d.ContentType)
            .ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult(counts);
    }
}
