using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Forum;

public sealed class TopicModel : ForumTopicPageModel
{
    private readonly IForumRepository forumRepository;
    private readonly IForumWriteRepository forumWriteRepository;
    private readonly UgcHtml ugcHtml;
    private readonly ForumPostRateLimiter rateLimiter;
    private readonly TimeProvider timeProvider;

    public TopicModel(
        IForumRepository forumRepository,
        IForumWriteRepository forumWriteRepository,
        UgcHtml ugcHtml,
        ForumPostRateLimiter rateLimiter,
        TimeProvider timeProvider)
        : base(forumRepository)
    {
        this.forumRepository = forumRepository;
        this.forumWriteRepository = forumWriteRepository;
        this.ugcHtml = ugcHtml;
        this.rateLimiter = rateLimiter;
        this.timeProvider = timeProvider;
    }

    [BindProperty]
    [Required]
    public string Body { get; set; } = string.Empty;

    public bool CanReply { get; private set; }

    public async Task<IActionResult> OnGetAsync(int topicId, string slug, CancellationToken cancellationToken)
    {
        var result = await LoadTopicPageAsync(topicId, slug, 1, cancellationToken);
        CanReply = await IsMemberAuthenticatedAsync();
        return result;
    }

    public async Task<IActionResult> OnPostAsync(int topicId, string slug, CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Challenge(MemberAuthenticationSchemes.MembersCookie);
        }

        var loadResult = await LoadTopicPageAsync(topicId, slug, 1, cancellationToken);
        if (loadResult is not PageResult)
        {
            return loadResult;
        }
        CanReply = true;

        var thread = await forumWriteRepository.GetThreadAsync(topicId, cancellationToken);
        if (thread is null)
        {
            return NotFound();
        }

        if (thread.IsLocked)
        {
            return Forbid();
        }

        var sanitizedBody = ugcHtml.Sanitize(Body);
        if (string.IsNullOrWhiteSpace(sanitizedBody))
        {
            ModelState.AddModelError(nameof(Body), "Body is required.");
        }

        if (!ModelState.IsValid)
        {
            Body = sanitizedBody;
            return Page();
        }

        if (!await rateLimiter.IsAllowedAsync(memberId.Value, cancellationToken))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        var postId = await forumWriteRepository.CreatePostAsync(
            new NewForumPost(
                topicId,
                memberId.Value,
                User.Identity?.Name ?? "Member",
                sanitizedBody,
                timeProvider.GetUtcNow()),
            cancellationToken);

        var updatedPage = await forumRepository.GetTopicPostsPageAsync(topicId, 1, 1, cancellationToken);
        var lastPage = updatedPage is null
            ? 1
            : Math.Max(1, ForumRoutes.GetPostsTotalPages(updatedPage.TotalCount, ForumRoutes.PostsPageSize));
        var redirectPath = Header is null
            ? ForumRoutes.GetTopicCanonicalPath(topicId, slug, lastPage)
            : ForumRoutes.GetTopicCanonicalPath(Header, lastPage);

        return Redirect(redirectPath + $"#post-{postId}");
    }

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var direct = ForumMember.GetMemberId(User);
        if (direct is not null)
        {
            return direct;
        }

        var memberCookie = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (memberCookie.Succeeded)
        {
            return ForumMember.GetMemberId(memberCookie.Principal!);
        }

        if (HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsEnvironment("Testing") == true)
        {
            var testMember = await HttpContext.AuthenticateAsync(TestMemberAuthHandler.SchemeName);
            if (testMember.Succeeded)
            {
                return ForumMember.GetMemberId(testMember.Principal!);
            }
        }

        return null;
    }

    private async Task<bool> IsMemberAuthenticatedAsync() =>
        await GetCurrentMemberIdAsync() is not null;
}
