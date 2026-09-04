using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class FanPerformanceSubmissionEntity
{
    public Guid Id { get; set; }

    public Guid SubmitterMemberId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Queen song covered. Submission metadata only — not a Q_STAGE_T column.</summary>
    public string CoveredSong { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Blob name of the original upload within <c>ugc-fan-performances</c>.</summary>
    public string BlobPath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }

    public string Status { get; set; } = FanPerformanceSubmissionStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewerEmail { get; set; }

    public string? ReviewNotes { get; set; }

    public string? RejectionReason { get; set; }

    public DateTimeOffset RightsDeclaredAt { get; set; }

    public string RightsDeclarationVersion { get; set; } = FanPerformanceSubmissionRights.DeclarationVersion;

    /// <summary>Q_STAGE_T id created when this submission is published.</summary>
    public int? PromotedStageId { get; set; }

    public MemberAccount? Submitter { get; set; }

    public ICollection<FanPerformanceSubmissionAuditLogEntity> AuditLogs { get; set; } =
        new List<FanPerformanceSubmissionAuditLogEntity>();
}
