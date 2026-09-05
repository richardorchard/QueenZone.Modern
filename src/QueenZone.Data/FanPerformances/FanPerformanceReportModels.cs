namespace QueenZone.Data;

public sealed record FanPerformanceReport(
    Guid Id,
    int StageId,
    Guid ReporterMemberId,
    string ReporterDisplayName,
    string Reason,
    DateTimeOffset CreatedAt,
    string Status,
    string? TitleSnapshot,
    string? PerformedBySnapshot,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAt);

public sealed record FanPerformanceReportListItem(
    Guid Id,
    int StageId,
    Guid ReporterMemberId,
    string ReporterDisplayName,
    string Reason,
    DateTimeOffset CreatedAt,
    string Status,
    string? TitleSnapshot,
    string? PerformedBySnapshot);

public sealed record FanPerformanceReportListPage(
    IReadOnlyList<FanPerformanceReportListItem> Items,
    int TotalCount,
    string StatusFilter);

public sealed record NewFanPerformanceReport(
    int StageId,
    Guid ReporterMemberId,
    string Reason,
    string? TitleSnapshot,
    string? PerformedBySnapshot);

public sealed record FanPerformanceReportCreateResult(
    bool Succeeded,
    Guid? ReportId,
    bool AlreadyReported,
    string? Error);
