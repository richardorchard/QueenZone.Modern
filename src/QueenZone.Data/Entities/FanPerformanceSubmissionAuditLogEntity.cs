using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class FanPerformanceSubmissionAuditLogEntity
{
    public long Id { get; set; }

    public Guid FanPerformanceSubmissionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ActorEmail { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string? Details { get; set; }

    public FanPerformanceSubmissionEntity? Submission { get; set; }
}
