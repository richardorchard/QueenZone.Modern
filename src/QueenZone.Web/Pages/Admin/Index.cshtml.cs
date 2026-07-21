using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin;

public sealed class IndexModel(
    IMemberAccountRepository memberAccountRepository,
    IPhotoSubmissionRepository photoSubmissionRepository,
    INewsSuggestionRepository newsSuggestionRepository,
    IArticleSubmissionRepository articleSubmissionRepository) : PageModel
{
    public MemberStats MemberStats { get; private set; } = null!;
    public IReadOnlyList<RecentLogin> RecentLogins { get; private set; } = [];
    public IReadOnlyList<DailyRegistration> DailyRegistrations { get; private set; } = [];
    public SubmissionQueueStats SubmissionQueue { get; private set; } = SubmissionQueueStats.Empty;

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ViewData["ShowAdminNav"] = true;
        base.OnPageHandlerExecuting(context);
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var utcNowOffset = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(utcNow.Year, utcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Await sequentially: all EF repositories share one scoped QueenZoneDbContext,
        // and DbContext is not safe for concurrent operations.
        MemberStats = await memberAccountRepository.GetStatsAsync(utcNow, cancellationToken);
        RecentLogins = await memberAccountRepository.GetRecentLoginsAsync(5, cancellationToken);
        DailyRegistrations = await memberAccountRepository.GetDailyRegistrationsAsync(
            DateOnly.FromDateTime(utcNow).AddDays(-29), cancellationToken);

        var photoCounts = await photoSubmissionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);
        var newsCounts = await newsSuggestionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);
        var articleCounts = await articleSubmissionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);

        var photoContributors = await photoSubmissionRepository.GetTopContributorsThisMonthAsync(
            monthStart, 10, cancellationToken);
        var newsContributors = await newsSuggestionRepository.GetTopContributorsThisMonthAsync(
            monthStart, 10, cancellationToken);
        var articleContributors = await articleSubmissionRepository.GetTopContributorsThisMonthAsync(
            monthStart, 10, cancellationToken);

        SubmissionQueue = new SubmissionQueueStats(
            photoCounts,
            newsCounts,
            articleCounts,
            CombineTopContributors(photoContributors, newsContributors, articleContributors, maxCount: 5));

        return Page();
    }

    private static IReadOnlyList<SubmissionContributor> CombineTopContributors(
        IReadOnlyList<SubmissionContributor> photos,
        IReadOnlyList<SubmissionContributor> news,
        IReadOnlyList<SubmissionContributor> articles,
        int maxCount)
    {
        return photos.Concat(news).Concat(articles)
            .GroupBy(c => c.MemberId)
            .Select(g => new SubmissionContributor(
                g.Key,
                g.Select(c => c.DisplayName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                    ?? "Unknown member",
                g.Sum(c => c.Count)))
            .OrderByDescending(c => c.Count)
            .Take(maxCount)
            .ToList();
    }
}
