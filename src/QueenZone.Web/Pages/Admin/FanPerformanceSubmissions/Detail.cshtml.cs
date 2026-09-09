using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FanPerformanceSubmissions;

public sealed class DetailModel(IFanPerformanceSubmissionRepository fanPerformanceSubmissionRepository)
    : AdminFanPerformanceSubmissionsPageModel
{
    public FanPerformanceSubmission? Submission { get; private set; }

    public IReadOnlyList<FanPerformanceSubmissionAuditEntry> AuditLog { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Submission = await fanPerformanceSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (Submission is null)
        {
            return NotFound();
        }

        AuditLog = await fanPerformanceSubmissionRepository.GetAuditLogsAsync(id, cancellationToken);
        StatusMessage = TempData["FanPerformanceSubmissionMessage"] as string;
        StatusMessageKind = TempData["FanPerformanceSubmissionMessageKind"] as string;
        ViewData["Title"] = $"Review fan performance — {Submission.Title}";
        Breadcrumbs = AdminBreadcrumbs.Page("Fan performance submissions", "/admin/fan-performance-submissions", "Review submission");
        return Page();
    }
}
