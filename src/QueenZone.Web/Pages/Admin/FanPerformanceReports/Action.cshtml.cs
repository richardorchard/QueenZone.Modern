using Microsoft.AspNetCore.Mvc;

namespace QueenZone.Web.Pages.Admin.FanPerformanceReports;

public sealed class ActionModel(FanPerformanceReportService reportService)
    : AdminFanPerformanceReportsPageModel
{
    public async Task<IActionResult> OnPostHideAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await reportService.HideAndResolveAsync(id, EditorEmail, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            TempData["FanPerformanceReportMessage"] = "Hidden the published performance and marked the report resolved.";
            TempData["FanPerformanceReportMessageKind"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["FanPerformanceReportMessage"] = ex.Message;
            TempData["FanPerformanceReportMessageKind"] = "error";
        }

        return Redirect($"/admin/fan-performance-reports/{id}");
    }

    public async Task<IActionResult> OnPostDismissAsync(Guid id, CancellationToken cancellationToken)
    {
        var updated = await reportService.DismissAsync(id, EditorEmail, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        TempData["FanPerformanceReportMessage"] = "Dismissed the report without hiding the performance.";
        TempData["FanPerformanceReportMessageKind"] = "success";
        return Redirect($"/admin/fan-performance-reports/{id}");
    }
}
