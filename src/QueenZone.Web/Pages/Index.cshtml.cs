using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages;

public sealed class IndexModel(
    PublicQueryCacheService publicQueryCache,
    NewsDiscussionComposer newsDiscussion,
    IQuoteRepository quoteRepository,
    IHomePollRepository homePollRepository,
    HomePollVoteService homePollVoteService,
    TimeProvider timeProvider) : PageModel
{
    /// <summary>Stock archive images cycled deterministically per article, since legacy
    /// article rows carry no per-item image (see <see cref="ArticleItem"/>).</summary>
    private static readonly string[] FeaturedArticleImages =
    [
        "/design-system/assets/img-studio.jpg",
        "/design-system/assets/img-portrait.jpg",
        "/design-system/assets/img-crowd.jpg",
        "/design-system/assets/img-stage.jpg",
    ];

    private const int FeaturedGalleryCount = 4;

    public IReadOnlyList<NewsArchiveItem> Latest { get; private set; } = [];

    public IReadOnlyList<QueenHistoryEvent> OnThisDay { get; private set; } = [];

    public bool IsOnThisDayFallback { get; private set; }

    public IReadOnlyList<HomeArticleTeaser> FeaturedArticles { get; private set; } = [];

    public IReadOnlyList<PhotoCategory> FeaturedGalleryCategories { get; private set; } = [];

    public QuoteItem? FeaturedQuote { get; private set; }

    public HomePollResults? HomePoll { get; private set; }

    public bool HomePollViewerCanVote { get; private set; }

    public string? HomePollError { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "QueenZone";
        ViewData["Description"] = "The complete fan resource for Queen – music, news, history, photography and more, from the Queenzone.com archive.";
        ViewData["CanonicalPath"] = "/";
        var latest = await publicQueryCache.GetLatestNewsAsync(5, cancellationToken);
        Latest = await newsDiscussion.ToArchiveItemsAsync(latest, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        OnThisDay = await publicQueryCache.GetOnThisDayAsync(today, 3, cancellationToken);

        if (OnThisDay.Count == 0)
        {
            OnThisDay = await publicQueryCache.GetAroundThisDayAsync(today, 7, 3, cancellationToken);
            IsOnThisDayFallback = OnThisDay.Count > 0;
        }

        var articles = await publicQueryCache.GetLatestArticlesAsync(ArticlesRoutes.HomeFeaturedCount, cancellationToken);
        FeaturedArticles = articles
            .Select((item, index) => new HomeArticleTeaser(
                FeaturedArticleImages[index % FeaturedArticleImages.Length],
                string.IsNullOrWhiteSpace(item.CategoryName) ? "Feature" : item.CategoryName,
                item.Title,
                item.Excerpt,
                item.PublishedAt.ToString("dd MMMM yyyy"),
                ArticlesRoutes.GetArticleDetailPath(item)))
            .ToList();

        var categories = await publicQueryCache.GetPhotoCategoriesAsync(cancellationToken);
        FeaturedGalleryCategories = categories
            .Where(category => !string.IsNullOrWhiteSpace(category.CoverThumbnailUrl))
            .Take(FeaturedGalleryCount)
            .ToList();

        FeaturedQuote = await quoteRepository.GetRandomPublishedAsync(cancellationToken);
        await LoadHomePollAsync(cancellationToken);
        HomePollError = TempData["HomePollError"] as string;
    }

    public async Task<IActionResult> OnPostVoteAsync(Guid optionId, CancellationToken cancellationToken)
    {
        var memberAuth = await HttpContext.AuthenticateMemberAsync();
        var memberId = ForumMember.GetMemberId(memberAuth.Principal);
        if (memberId is null)
        {
            return Unauthorized();
        }

        try
        {
            await homePollVoteService.CastVoteAsync(memberId.Value, optionId, cancellationToken);
        }
        catch (ForumPollVoteException ex)
        {
            TempData["HomePollError"] = ex.Message;
        }

        return Redirect("/#home-poll");
    }

    private async Task LoadHomePollAsync(CancellationToken cancellationToken)
    {
        var memberAuth = await HttpContext.AuthenticateMemberAsync();
        var memberId = ForumMember.GetMemberId(memberAuth.Principal);
        HomePoll = await homePollRepository.GetCurrentAsync(memberId, cancellationToken);
        HomePollViewerCanVote = memberId is not null
            && HomePoll is { IsClosed: false, ViewerHasVoted: false };
    }
}

public sealed record HomeArticleTeaser(
    string Image,
    string Category,
    string Title,
    string Excerpt,
    string Meta,
    string Href);
