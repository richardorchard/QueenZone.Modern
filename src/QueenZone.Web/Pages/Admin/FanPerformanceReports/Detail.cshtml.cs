using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FanPerformanceReports;

public sealed class DetailModel(IFanPerformanceReportRepository reportRepository)
    : AdminFanPerformanceReportsPageModel
{
    public FanPerformanceReport? Report { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Report = await reportRepository.GetByIdAsync(id, cancellationToken);
        if (Report is null)
        {
            return NotFound();
        }

        StatusMessage = TempData["FanPerformanceReportMessage"] as string;
        StatusMessageKind = TempData["FanPerformanceReportMessageKind"] as string;
        ViewData["Title"] = "Fan performance report";
        return Page();
    }
}
