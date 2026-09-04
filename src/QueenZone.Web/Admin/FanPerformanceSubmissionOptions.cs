using QueenZone.Data;

namespace QueenZone.Web;

public sealed class FanPerformanceSubmissionOptions
{
    public const string SectionName = "FanPerformanceSubmissions";

    public int StaleAfterDays { get; set; } = FanPerformanceDashboardCounts.DefaultStaleAfterDays;
}
