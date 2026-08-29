using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Forum;

public sealed class HideAuthorModel(
    IForumWriteRepository forumWriteRepository,
    IOptions<AdminOptions> adminOptions) : PageModel
{
    private readonly AdminOptions adminOptions = adminOptions.Value;

    public ForumEditablePost? Post { get; private set; }

    public AuthorForumContentCounts ForumContent { get; private set; } =
        new(string.Empty, 0, 0, 0, 0);

    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int postId, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync();
        if (admin is not null)
        {
            return admin;
        }

        var post = await forumWriteRepository.GetPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        Post = post;
        ForumContent = await forumWriteRepository.CountAuthorForumContentAsync(
            post.AuthorMemberId,
            post.AuthorDisplayName,
            cancellationToken);
        PopulatePage(post);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int postId, CancellationToken cancellationToken)
    {
        var admin = await RequireAdminAsync();
        if (admin is not null)
        {
            return admin;
        }

        var post = await forumWriteRepository.GetPostAsync(postId, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        await forumWriteRepository.HideAuthorForumContentAsync(
            post.AuthorMemberId,
            post.AuthorDisplayName,
            cancellationToken);

        var topicPath = ForumRoutes.GetTopicCanonicalPath(post.TopicId, post.TopicSubject);
        return Redirect(topicPath);
    }

    private async Task<IActionResult?> RequireAdminAsync()
    {
        var memberAuth = await ResolveMemberAuthAsync();
        if (memberAuth?.Principal is null || ForumMember.GetMemberId(memberAuth.Principal) is null)
        {
            return Challenge(MemberAuthenticationSchemes.MembersCookie);
        }

        if (!ForumPollEndpoints.IsAdmin(memberAuth.Principal, adminOptions))
        {
            ErrorMessage = "You do not have permission to hide forum content.";
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Page();
        }

        return null;
    }

    private void PopulatePage(ForumEditablePost post)
    {
        var topicPath = ForumRoutes.GetTopicCanonicalPath(post.TopicId, post.TopicSubject);
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Forum", "/forum"),
            new BreadcrumbItem(post.TopicSubject, topicPath),
            new BreadcrumbItem("Hide author", $"/forum/hide-author?postId={post.PostId}"),
        ];
        ViewData["Title"] = "Hide all by this author";
        ViewData["CanonicalPath"] = $"/forum/hide-author?postId={post.PostId}";
        ViewData["Robots"] = "noindex,nofollow";
    }

    private async Task<AuthenticateResult?> ResolveMemberAuthAsync()
    {
        var directId = ForumMember.GetMemberId(User);
        if (directId is not null)
        {
            return AuthenticateResult.Success(new AuthenticationTicket(User, MemberAuthenticationSchemes.MembersCookie));
        }

        var memberAuth = await HttpContext.AuthenticateMemberAsync();
        return memberAuth.Succeeded ? memberAuth : null;
    }
}
