namespace QueenZone.Data;

public static class PrivateMessageReportAuditActions
{
    public const string Viewed = "Viewed";

    public const string StatusChanged = "StatusChanged";
}

public sealed record PrivateMessageReportListItem(
    Guid Id,
    Guid ReporterMemberId,
    string ReporterDisplayName,
    Guid ReportedMemberId,
    string ReportedDisplayName,
    string? Reason,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record PrivateMessageReportListPage(
    IReadOnlyList<PrivateMessageReportListItem> Items,
    int TotalCount,
    string? StatusFilter);

public sealed record PrivateMessageReportAuditLog(
    long Id,
    Guid ReportId,
    string Action,
    string ActorEmail,
    DateTimeOffset OccurredAt,
    string? Details);

public sealed record PrivateMessageReportReviewContext(
    PrivateMessageReport Report,
    string ReporterDisplayName,
    string ReportedDisplayName,
    string? ReviewerEmail,
    DateTimeOffset? ReviewedAt,
    string? ReviewNotes,
    IReadOnlyList<PrivateMessageReportAuditLog> AuditLogs);
