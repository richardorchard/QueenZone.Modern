using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Records moderator/admin access to a <see cref="PrivateMessageReportEntity"/>: viewing its
/// snapshotted content and any status transition. Retained independently of the report itself
/// (see ADR 0015) so access history survives report purges.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PrivateMessageReportAuditLogEntity
{
    public long Id { get; set; }

    public Guid ReportId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ActorEmail { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string? Details { get; set; }
}
