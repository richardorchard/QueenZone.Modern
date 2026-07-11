namespace QueenZone.Data;

public interface IForumAttachmentRepository
{
    Task AddAttachmentsAsync(
        int legacyPostId,
        IEnumerable<NewForumAttachment> attachments,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoredForumAttachment>> GetByLegacyPostIdsAsync(
        IReadOnlyCollection<int> legacyPostIds,
        CancellationToken cancellationToken = default);

    Task<StoredForumAttachment?> GetAsync(
        int legacyPostId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<LegacyForumAttachmentLookup?> GetLegacyAsync(
        int legacyPostId,
        CancellationToken cancellationToken = default);

    Task IncrementDownloadCountAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
