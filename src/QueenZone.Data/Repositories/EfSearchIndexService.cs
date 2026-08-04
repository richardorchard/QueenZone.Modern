using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

/// <summary>Plain EF Core writes against the modern <c>SearchDocument</c> table.</summary>
public sealed class EfSearchIndexService(QueenZoneDbContext dbContext) : ISearchIndexService
{
    public async Task UpsertAsync(SearchDocumentEntity document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.SourceKey);

        var existing = await dbContext.SearchDocuments
            .SingleOrDefaultAsync(d => d.SourceKey == document.SourceKey, cancellationToken);

        document.IndexedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            document.Id = document.Id == Guid.Empty ? Guid.NewGuid() : document.Id;
            dbContext.SearchDocuments.Add(document);
        }
        else
        {
            existing.ContentType = document.ContentType;
            existing.Title = document.Title;
            existing.Body = document.Body;
            existing.Summary = document.Summary;
            existing.Url = document.Url;
            existing.PublishedAt = document.PublishedAt;
            existing.ImageUrl = document.ImageUrl;
            existing.Category = document.Category;
            existing.AuthorDisplayName = document.AuthorDisplayName;
            existing.IndexedAt = document.IndexedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string sourceKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceKey);

        await dbContext.SearchDocuments
            .Where(d => d.SourceKey == sourceKey)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ReplaceContentTypeAsync(
        string contentType,
        IReadOnlyList<SearchDocumentEntity> documents,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(documents);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.SearchDocuments
            .Where(d => d.ContentType == contentType)
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var document in documents)
        {
            document.Id = document.Id == Guid.Empty ? Guid.NewGuid() : document.Id;
            document.ContentType = contentType;
            document.IndexedAt = now;
            dbContext.SearchDocuments.Add(document);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
