namespace QueenZone.Data;

public sealed class InMemoryForumAttachmentRepository : IForumAttachmentRepository
{
    private readonly object sync = new();
    private readonly List<StoredForumAttachment> attachments = [];
    private readonly Dictionary<int, LegacyForumAttachmentLookup> legacyByPostId = new();

    public Task AddAttachmentsAsync(
        int legacyPostId,
        IEnumerable<NewForumAttachment> attachments,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            foreach (var item in attachments)
            {
                this.attachments.Add(new StoredForumAttachment(
                    Guid.NewGuid(),
                    PostId: legacyPostId,
                    legacyPostId,
                    item.OriginalFileName.Trim(),
                    item.BlobPath.Trim(),
                    item.ContainerName.Trim(),
                    item.FileSizeBytes,
                    item.MimeType.Trim(),
                    item.UploadedAt,
                    DownloadCount: 0));
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoredForumAttachment>> GetByLegacyPostIdsAsync(
        IReadOnlyCollection<int> legacyPostIds,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var idSet = legacyPostIds.ToHashSet();
            IReadOnlyList<StoredForumAttachment> result = attachments
                .Where(item => idSet.Contains(item.LegacyPostId))
                .OrderBy(item => item.UploadedAt)
                .ThenBy(item => item.OriginalFileName)
                .ToList();
            return Task.FromResult(result);
        }
    }

    public Task<StoredForumAttachment?> GetAsync(
        int legacyPostId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var match = attachments.SingleOrDefault(item =>
                item.Id == attachmentId && item.LegacyPostId == legacyPostId);
            return Task.FromResult(match);
        }
    }

    public Task<LegacyForumAttachmentLookup?> GetLegacyAsync(
        int legacyPostId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (legacyByPostId.TryGetValue(legacyPostId, out var seeded))
            {
                return Task.FromResult<LegacyForumAttachmentLookup?>(seeded);
            }

            // Fall back to sample posts (legacy import shape).
            foreach (var topicId in new[] { 1001, 1002, 1003 })
            {
                foreach (var post in SampleForumData.CreateSeedPosts(topicId))
                {
                    if (post.Id != legacyPostId || post.Attachments is not { Count: > 0 })
                    {
                        continue;
                    }

                    var first = post.Attachments[0];
                    return Task.FromResult<LegacyForumAttachmentLookup?>(
                        new LegacyForumAttachmentLookup(legacyPostId, first.FileName, first.FileSizeBytes));
                }
            }

            return Task.FromResult<LegacyForumAttachmentLookup?>(null);
        }
    }

    public Task IncrementDownloadCountAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var index = attachments.FindIndex(item => item.Id == attachmentId);
            if (index < 0)
            {
                return Task.CompletedTask;
            }

            var current = attachments[index];
            attachments[index] = current with { DownloadCount = current.DownloadCount + 1 };
            return Task.CompletedTask;
        }
    }

    /// <summary>Test helper: seed a legacy attachment lookup without a modern row.</summary>
    public void SeedLegacy(LegacyForumAttachmentLookup lookup)
    {
        lock (sync)
        {
            legacyByPostId[lookup.LegacyPostId] = lookup;
        }
    }

    public IReadOnlyList<StoredForumAttachment> GetAll()
    {
        lock (sync)
        {
            return attachments.ToList();
        }
    }
}
