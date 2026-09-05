using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FanPerformanceReports;

public sealed class IndexModel(IFanPerformanceReportRepository reportRepository)
    : AdminFanPerformanceReportsPageModel
{
    public FanPerformanceReportListPage List { get; private set; } =
        new([], 0, FanPerformanceReportStatus.Open);

    public int PageNumber { get; private set; } = 1;

    public string StatusFilter { get; private set; } = FanPerformanceReportStatus.Open;

    public async Task OnGetAsync(
        string? status = FanPerformanceReportStatus.Open,
        int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        StatusFilter = string.IsNullOrWhiteSpace(status) ? FanPerformanceReportStatus.Open : status;
        List = await reportRepository.ListAsync(
            StatusFilter,
            PageNumber,
            FanPerformanceReportLimits.ListPageSize,
            cancellationToken);
        ViewData["Title"] = "Fan performance reports";
    }
}
