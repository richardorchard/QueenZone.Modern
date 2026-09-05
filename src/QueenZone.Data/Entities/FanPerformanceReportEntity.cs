using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class FanPerformanceReportEntity
{
    public Guid Id { get; set; }

    public int StageId { get; set; }

    public Guid ReporterMemberId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string Status { get; set; } = FanPerformanceReportStatus.Open;

    public string? TitleSnapshot { get; set; }

    public string? PerformedBySnapshot { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public MemberAccount? Reporter { get; set; }
}
