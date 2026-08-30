using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class TriviaFactSubmissionEntity
{
    public Guid Id { get; set; }

    public Guid SubmitterMemberId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? Category { get; set; }

    public string? Difficulty { get; set; }

    public string? SourceNote { get; set; }

    public string Status { get; set; } = TriviaFactSubmissionStatus.Pending;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewerEmail { get; set; }

    public string? ReviewNotes { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>TriviaFacts.Id created when this suggestion was approved and published.</summary>
    public int? PromotedTriviaId { get; set; }

    public MemberAccount? Submitter { get; set; }

    public ICollection<TriviaFactSubmissionAuditLogEntity> AuditLogs { get; set; } =
        new List<TriviaFactSubmissionAuditLogEntity>();
}
