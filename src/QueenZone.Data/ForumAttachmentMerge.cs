using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

/// <summary>
/// Merges modern <see cref="ForumPostAttachmentEntity"/> rows into post DTOs that already
/// carry optional legacy import attachments.
/// </summary>
public static class ForumAttachmentMerge
{
    public static async Task<List<ForumPostItem>> MergeModernAsync(
        QueenZoneDbContext dbContext,
        List<ForumPostItem> posts,
        CancellationToken cancellationToken = default)
    {
        if (posts.Count == 0)
        {
            return posts;
        }

        var legacyIds = posts.Select(post => post.Id).Distinct().ToArray();
        // Order in memory: SQLite cannot translate DateTimeOffset OrderBy in all EF versions.
        var modern = await dbContext.ForumPostAttachments
            .AsNoTracking()
            .Where(row => legacyIds.Contains(row.LegacyPostId))
            .ToListAsync(cancellationToken);

        modern = modern
            .OrderBy(row => row.UploadedAt)
            .ThenBy(row => row.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (modern.Count == 0)
        {
            return posts;
        }

        var byPost = modern
            .GroupBy(row => row.LegacyPostId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ForumPostAttachment>)group
                    .Select(row => ForumPostAttachment.FromStored(new StoredForumAttachment(
                        row.Id,
                        row.PostId,
                        row.LegacyPostId,
                        row.OriginalFileName,
                        row.BlobPath,
                        row.ContainerName,
                        row.FileSizeBytes,
                        row.MimeType,
                        row.UploadedAt,
                        row.DownloadCount)))
                    .ToList());

        return Merge(posts, byPost);
    }

    public static List<ForumPostItem> Merge(
        IReadOnlyList<ForumPostItem> posts,
        IReadOnlyDictionary<int, IReadOnlyList<ForumPostAttachment>> modernByLegacyPostId)
    {
        var result = new List<ForumPostItem>(posts.Count);
        foreach (var post in posts)
        {
            if (!modernByLegacyPostId.TryGetValue(post.Id, out var modern) || modern.Count == 0)
            {
                result.Add(post);
                continue;
            }

            var combined = new List<ForumPostAttachment>();
            if (post.Attachments is { Count: > 0 })
            {
                combined.AddRange(post.Attachments);
            }

            combined.AddRange(modern);
            result.Add(post with { Attachments = combined });
        }

        return result;
    }

    public static async Task<List<ForumPostItem>> MergeViaRepositoryAsync(
        IForumAttachmentRepository repository,
        List<ForumPostItem> posts,
        CancellationToken cancellationToken = default)
    {
        if (posts.Count == 0)
        {
            return posts;
        }

        var stored = await repository.GetByLegacyPostIdsAsync(
            posts.Select(post => post.Id).ToArray(),
            cancellationToken);
        if (stored.Count == 0)
        {
            return posts;
        }

        var byPost = stored
            .GroupBy(item => item.LegacyPostId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ForumPostAttachment>)group
                    .Select(ForumPostAttachment.FromStored)
                    .ToList());

        return Merge(posts, byPost);
    }
}
