using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.FanPerformanceSubmissions;

public sealed class IndexModel(IFanPerformanceSubmissionRepository fanPerformanceSubmissionRepository)
    : AdminFanPerformanceSubmissionsPageModel
{
    public IReadOnlyList<FanPerformanceSubmissionListItem> Submissions { get; private set; } = [];

    public int PageNumber { get; private set; } = 1;

    public async Task OnGetAsync(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        Submissions = await fanPerformanceSubmissionRepository.GetPendingAsync(PageNumber, 50, cancellationToken);
        ViewData["Title"] = "Fan performance submissions";
    }

    public static string FormatDuration(int? seconds)
    {
        if (seconds is null or < 0)
        {
            return "—";
        }

        var minutes = seconds.Value / 60;
        var remainder = seconds.Value % 60;
        return $"{minutes}:{remainder:D2}";
    }
}
