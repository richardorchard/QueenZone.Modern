using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfForumAttachmentRepository(QueenZoneDbContext dbContext) : IForumAttachmentRepository
{
    public async Task AddAttachmentsAsync(
        int legacyPostId,
        IEnumerable<NewForumAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        var list = attachments.ToList();
        if (list.Count == 0)
        {
            return;
        }

        var post = await dbContext.ModernForumPosts
            .SingleOrDefaultAsync(item => item.LegacyPostId == legacyPostId, cancellationToken)
            ?? throw new InvalidOperationException($"Forum post {legacyPostId} was not found.");

        foreach (var item in list)
        {
            dbContext.ForumPostAttachments.Add(new ForumPostAttachmentEntity
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                LegacyPostId = legacyPostId,
                OriginalFileName = Truncate(item.OriginalFileName, 255),
                BlobPath = Truncate(item.BlobPath, 512),
                ContainerName = Truncate(item.ContainerName, 64),
                FileSizeBytes = item.FileSizeBytes,
                MimeType = Truncate(item.MimeType, 100),
                UploadedAt = item.UploadedAt,
                DownloadCount = 0,
            });
        }

        post.AttachCount = Math.Max(post.AttachCount, 0) + list.Count;
        post.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredForumAttachment>> GetByLegacyPostIdsAsync(
        IReadOnlyCollection<int> legacyPostIds,
        CancellationToken cancellationToken = default)
    {
        if (legacyPostIds.Count == 0)
        {
            return [];
        }

        var ids = legacyPostIds.Distinct().ToArray();
        // Order in memory: SQLite cannot translate DateTimeOffset OrderBy in all EF versions.
        var rows = await dbContext.ForumPostAttachments
            .AsNoTracking()
            .Where(row => ids.Contains(row.LegacyPostId))
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.UploadedAt)
            .ThenBy(row => row.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();
    }

    public async Task<StoredForumAttachment?> GetAsync(
        int legacyPostId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.ForumPostAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == attachmentId && item.LegacyPostId == legacyPostId,
                cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<LegacyForumAttachmentLookup?> GetLegacyAsync(
        int legacyPostId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.LegacyPostId == legacyPostId)
            .Select(post => new { post.LegacyPostId, post.Attachment, post.FileSize })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null || string.IsNullOrWhiteSpace(row.Attachment))
        {
            return null;
        }

        long? bytes = long.TryParse(row.FileSize?.Trim(), out var parsed) ? parsed : null;
        return new LegacyForumAttachmentLookup(row.LegacyPostId, row.Attachment.Trim(), bytes);
    }

    public async Task IncrementDownloadCountAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.ForumPostAttachments
            .SingleOrDefaultAsync(item => item.Id == attachmentId, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.DownloadCount += 1;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static StoredForumAttachment Map(ForumPostAttachmentEntity row) =>
        new(
            row.Id,
            row.PostId,
            row.LegacyPostId,
            row.OriginalFileName,
            row.BlobPath,
            row.ContainerName,
            row.FileSizeBytes,
            row.MimeType,
            row.UploadedAt,
            row.DownloadCount);

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
