using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Modern (non-legacy) forum post attachment stored in private UGC blob storage.
/// Legacy archive attachments remain on <see cref="ModernForumPostEntity.Attachment"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ForumPostAttachmentEntity
{
    public Guid Id { get; set; }

    /// <summary>Internal modern post row id (<see cref="ModernForumPostEntity.Id"/>).</summary>
    public long PostId { get; set; }

    /// <summary>Public post id used in topic URLs and download routes.</summary>
    public int LegacyPostId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Blob name within the container (not a full URL).</summary>
    public string BlobPath { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "ugc-forum";

    public long FileSizeBytes { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public DateTimeOffset UploadedAt { get; set; }

    public int DownloadCount { get; set; }

    public ModernForumPostEntity? Post { get; set; }
}
