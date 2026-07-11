namespace QueenZone.Data;

/// <summary>Metadata for a newly uploaded modern attachment ready to persist.</summary>
public sealed record NewForumAttachment(
    string OriginalFileName,
    string BlobPath,
    string ContainerName,
    long FileSizeBytes,
    string MimeType,
    DateTimeOffset UploadedAt);

/// <summary>Persisted modern attachment for reads and downloads.</summary>
public sealed record StoredForumAttachment(
    Guid Id,
    long PostId,
    int LegacyPostId,
    string OriginalFileName,
    string BlobPath,
    string ContainerName,
    long FileSizeBytes,
    string MimeType,
    DateTimeOffset UploadedAt,
    int DownloadCount)
{
    public string DownloadPath => ForumAttachmentPaths.DownloadPath(LegacyPostId, Id);

    public bool IsImage =>
        MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Legacy import attachment filename on a modern post row.</summary>
public sealed record LegacyForumAttachmentLookup(
    int LegacyPostId,
    string FileName,
    long? FileSizeBytes);
