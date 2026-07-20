using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class PhotoSubmissionEntity
{
    public Guid Id { get; set; }

    public Guid SubmitterMemberId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? SuggestedCategory { get; set; }

    public string? ApprovedCategory { get; set; }

    public int? ApproximateYear { get; set; }

    public DateOnly? ApproximateDate { get; set; }

    /// <summary>Blob name of the original upload within <c>ugc-photos</c>.</summary>
    public string BlobPath { get; set; } = string.Empty;

    /// <summary>Web-optimised WebP derivative (max 2000 px longest side).</summary>
    public string WebOptimizedBlobPath { get; set; } = string.Empty;

    /// <summary>400×400 WebP thumbnail.</summary>
    public string ThumbnailBlobPath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public int? ImageWidthPx { get; set; }

    public int? ImageHeightPx { get; set; }

    public string Status { get; set; } = PhotoSubmissionStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewerEmail { get; set; }

    public string? ReviewNotes { get; set; }

    public string? RejectionReason { get; set; }

    public MemberAccount? Submitter { get; set; }

    public ICollection<PhotoSubmissionAuditLogEntity> AuditLogs { get; set; } =
        new List<PhotoSubmissionAuditLogEntity>();
}
