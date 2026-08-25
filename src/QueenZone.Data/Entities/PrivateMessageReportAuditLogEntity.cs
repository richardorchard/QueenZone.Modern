using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Audit of admin access and decisions for a private-message report.
/// Written on content view as well as status changes.
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

    public PrivateMessageReportEntity? Report { get; set; }
}
