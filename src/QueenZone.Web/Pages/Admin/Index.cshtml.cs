using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin;

public sealed class IndexModel(
    IMemberAccountRepository memberAccountRepository,
    IPhotoSubmissionRepository photoSubmissionRepository,
    INewsSuggestionRepository newsSuggestionRepository,
    IArticleSubmissionRepository articleSubmissionRepository,
    INewsDiscoveryRepository newsDiscoveryRepository,
    INewsRepository newsRepository,
    IArticlesRepository articlesRepository) : PageModel
{
    public MemberStats MemberStats { get; private set; } = null!;
    public IReadOnlyList<RecentLogin> RecentLogins { get; private set; } = [];
    public IReadOnlyList<DailyRegistration> DailyRegistrations { get; private set; } = [];
    public SubmissionQueueStats SubmissionQueue { get; private set; } = SubmissionQueueStats.Empty;
    public ContentPipelineStats ContentPipeline { get; private set; } = ContentPipelineStats.Empty;

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

        var memberStatsTask = memberAccountRepository.GetStatsAsync(utcNow, cancellationToken);
        var recentLoginsTask = memberAccountRepository.GetRecentLoginsAsync(5, cancellationToken);
        var dailyRegistrationsTask = memberAccountRepository.GetDailyRegistrationsAsync(
            DateOnly.FromDateTime(utcNow).AddDays(-29), cancellationToken);

        var photoCountsTask = photoSubmissionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);
        var newsCountsTask = newsSuggestionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);
        var articleCountsTask = articleSubmissionRepository.GetDashboardCountsAsync(utcNowOffset, cancellationToken);

        var photoContributorsTask = photoSubmissionRepository.GetTopContributorsThisMonthAsync(monthStart, 10, cancellationToken);
        var newsContributorsTask = newsSuggestionRepository.GetTopContributorsThisMonthAsync(monthStart, 10, cancellationToken);
        var articleContributorsTask = articleSubmissionRepository.GetTopContributorsThisMonthAsync(monthStart, 10, cancellationToken);

        var pendingCandidatesTask = newsDiscoveryRepository.CountCandidatesAsync(
            NewsCandidateStatus.NeedsReview, cancellationToken);
        var aiPipelineTask = newsDiscoveryRepository.GetAiPipelineHealthAsync(utcNow, cancellationToken);
        var latestNewsTask = newsRepository.GetLatestAsync(10, cancellationToken);
        var latestArticlesTask = articlesRepository.GetLatestAsync(10, cancellationToken);

        await Task.WhenAll(
            memberStatsTask, recentLoginsTask, dailyRegistrationsTask,
            photoCountsTask, newsCountsTask, articleCountsTask,
            photoContributorsTask, newsContributorsTask, articleContributorsTask,
            pendingCandidatesTask, aiPipelineTask, latestNewsTask, latestArticlesTask);

        MemberStats = await memberStatsTask;
        RecentLogins = await recentLoginsTask;
        DailyRegistrations = await dailyRegistrationsTask;

        var topContributors = CombineTopContributors(
            await photoContributorsTask,
            await newsContributorsTask,
            await articleContributorsTask,
            maxCount: 5);

        SubmissionQueue = new SubmissionQueueStats(
            await photoCountsTask,
            await newsCountsTask,
            await articleCountsTask,
            topContributors);

        ContentPipeline = new ContentPipelineStats(
            await pendingCandidatesTask,
            await aiPipelineTask,
            MergeRecentlyPublished(await latestNewsTask, await latestArticlesTask, maxCount: 10));

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
                g.First(c => !string.IsNullOrWhiteSpace(c.DisplayName)).DisplayName
                    is { } name ? name : "Unknown member",
                g.Sum(c => c.Count)))
            .OrderByDescending(c => c.Count)
            .Take(maxCount)
            .ToList();
    }

    internal static IReadOnlyList<RecentlyPublishedDashboardItem> MergeRecentlyPublished(
        IReadOnlyList<NewsItem> news,
        IReadOnlyList<ArticleItem> articles,
        int maxCount)
    {
        return news
            .Select(item => new RecentlyPublishedDashboardItem(
                item.Title,
                item.PublishedAt,
                "News",
                NewsRoutes.GetNewsDetailPath(item)))
            .Concat(articles.Select(item => new RecentlyPublishedDashboardItem(
                item.Title,
                item.PublishedAt,
                "Article",
                ArticlesRoutes.GetArticleDetailPath(item))))
            .OrderByDescending(item => item.PublishedAt)
            .Take(maxCount)
            .ToList();
    }
}
