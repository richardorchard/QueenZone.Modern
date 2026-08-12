using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Members;

public sealed class ProfileModel(
    IMemberAccountRepository memberAccountRepository,
    IMemberPublicActivityRepository activityRepository,
    PrivateMessageService privateMessageService) : PageModel
{
    public const int ActivityPageSize = 20;

    public const string StatusMessageKey = "MemberProfileStatus";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public MemberAccount? Member { get; private set; }

    public Guid? CurrentMemberId { get; private set; }

    public bool CanMessage { get; private set; }

    public bool IsSignedIn { get; private set; }

    public bool HasBlockedMember { get; private set; }

    public string? StatusMessage { get; private set; }

    public IReadOnlyList<MemberActivityViewModel> Activity { get; private set; } = [];

    public ArchivePaginationViewModel? Pagination { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid memberId, CancellationToken cancellationToken)
    {
        if (PageNumber < 1)
        {
            return NotFound();
        }

        Member = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (Member is null || Member.PersonalDataPurgedAt is not null)
        {
            return NotFound();
        }

        CurrentMemberId = await GetCurrentMemberIdAsync();
        IsSignedIn = CurrentMemberId is not null;
        CanMessage = await privateMessageService.CanMessageAsync(
            CurrentMemberId,
            Member.Id,
            cancellationToken);
        if (CurrentMemberId is Guid current && current != Member.Id)
        {
            HasBlockedMember = await privateMessageService.HasBlockedAsync(
                current,
                Member.Id,
                cancellationToken);
        }

        StatusMessage = TempData[StatusMessageKey] as string;

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

    public async Task<IActionResult> OnPostBlockAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var currentMemberId = await GetCurrentMemberIdAsync();
        if (currentMemberId is null)
        {
            return Challenge();
        }

        var result = await privateMessageService.BlockAsync(
            currentMemberId.Value,
            memberId,
            cancellationToken);
        if (!result.Succeeded)
        {
            if (string.Equals(result.ErrorMessage, "Member was not found.", StringComparison.Ordinal))
            {
                return NotFound();
            }

            TempData[StatusMessageKey] = result.ErrorMessage ?? "Unable to block member.";
            return RedirectToPage(new { memberId });
        }

        TempData[StatusMessageKey] = "Member blocked. They can no longer send you private messages.";
        return RedirectToPage(new { memberId });
    }

    public async Task<IActionResult> OnPostUnblockAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var currentMemberId = await GetCurrentMemberIdAsync();
        if (currentMemberId is null)
        {
            return Challenge();
        }

        var member = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        await privateMessageService.UnblockAsync(currentMemberId.Value, memberId, cancellationToken);
        TempData[StatusMessageKey] = "Member unblocked.";
        return RedirectToPage(new { memberId });
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

        var memberAuth = await HttpContext.AuthenticateMemberAsync();
        return memberAuth.Succeeded ? ForumMember.GetMemberId(memberAuth.Principal) : null;
    }
}

public sealed record MemberActivityViewModel(
    string Type,
    string Title,
    string Summary,
    DateTimeOffset PublishedAt,
    string Href);
