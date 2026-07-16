using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Forum;

public abstract class ForumTopicPageModel : PageModel
{
    private readonly IForumRepository forumRepository;
    private readonly ForumOptions forumOptions;
    private readonly AdminOptions adminOptions;
    private readonly TimeProvider timeProvider;

    protected ForumTopicPageModel(
        IForumRepository forumRepository,
        IOptions<ForumOptions> forumOptions,
        IOptions<AdminOptions> adminOptions,
        TimeProvider timeProvider)
    {
        this.forumRepository = forumRepository;
        this.forumOptions = forumOptions.Value;
        this.adminOptions = adminOptions.Value;
        this.timeProvider = timeProvider;
    }

    public ForumThreadHeader? Header { get; private set; }

    public IReadOnlyList<ForumPostViewModel> Posts { get; private set; } = [];

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalPosts { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    protected async Task<IActionResult> LoadTopicPageAsync(
        int topicId,
        string slug,
        int page,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return NotFound();
        }

        var topicPage = await forumRepository.GetTopicPostsPageAsync(
            topicId,
            page,
            ForumRoutes.PostsPageSize,
            cancellationToken);
        if (topicPage is null)
        {
            return NotFound();
        }

        var header = PublicContentMapper.ToForumThreadHeader(topicPage.Header);
        var canonicalSlug = NewsSlug.Slugify(topicPage.Header.Title);
        if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectPermanent(ForumRoutes.GetTopicCanonicalPath(header, page));
        }

        var totalPages = ForumRoutes.GetPostsTotalPages(topicPage.TotalCount, ForumRoutes.PostsPageSize);
        if (topicPage.TotalCount == 0)
        {
            if (page > 1)
            {
                return NotFound();
            }
        }
        else if (page > totalPages)
        {
            return NotFound();
        }

        var memberAuth = await ResolveMemberAuthAsync();
        var memberId = ForumMember.GetMemberId(memberAuth?.Principal);
        var isAdmin = memberAuth?.Principal is not null
            && ForumPollEndpoints.IsAdmin(memberAuth.Principal, adminOptions);
        var utcNow = timeProvider.GetUtcNow();

        Header = header;
        Posts = topicPage.Posts
            .Select(PublicContentMapper.ToForumPostViewModel)
            .Select(post => PublicContentMapper.WithEditState(
                post,
                memberId,
                isAdmin,
                forumOptions.PostEditWindowMinutes,
                utcNow))
            .ToList();
        CurrentPage = page;
        TotalPages = totalPages;
        TotalPosts = topicPage.TotalCount;
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Forum", "/forum"),
            new BreadcrumbItem(header.ForumName, header.CategoryPath),
            new BreadcrumbItem(header.Title, header.DetailPath),
        ];

        ViewData["Title"] = ForumRoutes.GetTopicPageTitle(header, page);
        ViewData["CanonicalPath"] = ForumRoutes.GetTopicCanonicalPath(header, page);
        ViewData["Description"] = $"Read-only Queenzone forum archive thread in {header.ForumName}.";

        if (page > 1)
        {
            ViewData["PrevPath"] = ForumRoutes.GetTopicCanonicalPath(header, page - 1);
        }

        if (totalPages > 0 && page < totalPages)
        {
            ViewData["NextPath"] = ForumRoutes.GetTopicCanonicalPath(header, page + 1);
        }

        return Page();
    }

    protected async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var auth = await ResolveMemberAuthAsync();
        return ForumMember.GetMemberId(auth?.Principal);
    }

    protected async Task<AuthenticateResult?> ResolveMemberAuthAsync()
    {
        var directId = ForumMember.GetMemberId(User);
        if (directId is not null)
        {
            return AuthenticateResult.Success(new AuthenticationTicket(User, MemberAuthenticationSchemes.MembersCookie));
        }

        var memberCookie = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (memberCookie.Succeeded)
        {
            return memberCookie;
        }

        if (HttpContext.RequestServices.GetService<IHostEnvironment>()?.IsEnvironment("Testing") == true)
        {
            var testMember = await HttpContext.AuthenticateAsync(TestMemberAuthHandler.SchemeName);
            if (testMember.Succeeded)
            {
                return testMember;
            }
        }

        return null;
    }
}
