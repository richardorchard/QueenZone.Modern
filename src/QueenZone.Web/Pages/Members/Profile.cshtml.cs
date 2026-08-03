using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Members;

public sealed class ProfileModel(
    IMemberAccountRepository memberAccountRepository,
    IMemberPublicActivityRepository activityRepository) : PageModel
{
    public const int ActivityPageSize = 20;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public MemberAccount? Member { get; private set; }

    public Guid? CurrentMemberId { get; private set; }

    public bool CanMessage { get; private set; }

    public bool IsSignedIn { get; private set; }

    public IReadOnlyList<MemberActivityViewModel> Activity { get; private set; } = [];

    public ArchivePaginationViewModel? Pagination { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid memberId, CancellationToken cancellationToken)
    {
        if (PageNumber < 1)
        {
            return NotFound();
        }

        Member = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (Member is null)
        {
            return NotFound();
        }

        CurrentMemberId = await GetCurrentMemberIdAsync();
        IsSignedIn = CurrentMemberId is not null;
        CanMessage = PrivateMessageService.CanMessage(CurrentMemberId, Member.Id);
        var activityPage = await activityRepository.GetPageAsync(
            memberId,
            PageNumber,
            ActivityPageSize,
            cancellationToken);
        var totalPages = ArchivePagination.GetTotalPages(activityPage.TotalCount, ActivityPageSize);
        if (totalPages > 0 && PageNumber > totalPages)
        {
            return NotFound();
        }

        Activity = activityPage.Items.Select(ToViewModel).ToList();
        Pagination = ArchivePagination.BuildViewModel(
            "Member activity pagination",
            PageNumber,
            totalPages,
            page => page <= 1 ? $"/members/{memberId}" : $"/members/{memberId}?pageNumber={page}");
        ViewData["Title"] = Member.DisplayName;
        ViewData["CanonicalPath"] = PageNumber <= 1
            ? $"/members/{memberId}"
            : $"/members/{memberId}?pageNumber={PageNumber}";
        return Page();
    }

    private static MemberActivityViewModel ToViewModel(MemberPublicActivityItem item)
    {
        var summary = NewsArticleContent.ToPlainText(item.Summary ?? string.Empty);
        if (summary.Length > 220)
        {
            summary = summary[..217].TrimEnd() + "...";
        }

        return new MemberActivityViewModel(
            item.Type,
            item.Title,
            summary,
            item.PublishedAt,
            GetHref(item));
    }

    private static string GetHref(MemberPublicActivityItem item) => item.Type switch
    {
        MemberPublicActivityType.ForumPost when item.ParentId is int topicId =>
            $"{ForumRoutes.GetTopicCanonicalPath(topicId, item.Slug ?? item.Title)}#post-{item.ContentId}",
        MemberPublicActivityType.Article when !string.IsNullOrWhiteSpace(item.Slug) =>
            ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug),
        MemberPublicActivityType.News when item.ContentId is int newsId =>
            NewsRoutes.GetNewsDetailPath(newsId, item.Title, item.Slug),
        MemberPublicActivityType.Photo when !string.IsNullOrWhiteSpace(item.Category) =>
            $"/photography?category={Uri.EscapeDataString(item.Category)}",
        MemberPublicActivityType.Photo => "/photography",
        _ => "/",
    };

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var directId = ForumMember.GetMemberId(User);
        if (directId is not null)
        {
            return directId;
        }

        var memberCookie = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (memberCookie.Succeeded)
        {
            return ForumMember.GetMemberId(memberCookie.Principal);
        }

        if (HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsEnvironment("Testing") == true)
        {
            var testMember = await HttpContext.AuthenticateAsync(TestMemberAuthHandler.SchemeName);
            if (testMember.Succeeded)
            {
                return ForumMember.GetMemberId(testMember.Principal);
            }
        }

        return null;
    }
}

public sealed record MemberActivityViewModel(
    string Type,
    string Title,
    string Summary,
    DateTimeOffset PublishedAt,
    string Href);
