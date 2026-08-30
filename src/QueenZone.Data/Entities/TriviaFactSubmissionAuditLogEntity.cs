using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class TriviaFactSubmissionAuditLogEntity
{
    public long Id { get; set; }

    public Guid TriviaFactSubmissionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ActorEmail { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string? Details { get; set; }

    public TriviaFactSubmissionEntity? Submission { get; set; }
}
