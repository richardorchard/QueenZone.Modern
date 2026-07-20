using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin;

public sealed class IndexModel(IMemberAccountRepository memberAccountRepository) : PageModel
{
    public MemberStats MemberStats { get; private set; } = null!;
    public IReadOnlyList<RecentLogin> RecentLogins { get; private set; } = [];
    public IReadOnlyList<DailyRegistration> DailyRegistrations { get; private set; } = [];

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ViewData["ShowAdminNav"] = true;
        base.OnPageHandlerExecuting(context);
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        MemberStats = await memberAccountRepository.GetStatsAsync(utcNow, cancellationToken);
        RecentLogins = await memberAccountRepository.GetRecentLoginsAsync(5, cancellationToken);
        DailyRegistrations = await memberAccountRepository.GetDailyRegistrationsAsync(
            DateOnly.FromDateTime(utcNow).AddDays(-29), cancellationToken);
        return Page();
    }
}
