using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Forum;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy)]
public sealed class NewThreadModel(
    IForumRepository forumRepository,
    IForumWriteRepository forumWriteRepository,
    UgcHtml ugcHtml,
    ForumPostRateLimiter rateLimiter,
    TimeProvider timeProvider) : PageModel
{
    [BindProperty]
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Body { get; set; } = string.Empty;

    public ForumCategorySummary? Category { get; private set; }

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var category = await ResolveCategoryAsync(categorySlug, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        PopulatePage(category);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var category = await ResolveCategoryAsync(categorySlug, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        PopulatePage(category);

        var memberId = ForumMember.GetMemberId(User);
        if (memberId is null)
        {
            return Challenge();
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

        var threadId = await forumWriteRepository.CreateThreadAsync(
            new NewForumThread(
                category.Id,
                memberId.Value,
                User.Identity?.Name ?? "Member",
                Subject,
                sanitizedBody,
                timeProvider.GetUtcNow()),
            cancellationToken);

        return Redirect(ForumRoutes.GetTopicCanonicalPath(threadId, Subject));
    }

    private async Task<ForumCategorySummary?> ResolveCategoryAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var categories = await forumRepository.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(item =>
            string.Equals(NewsSlug.Slugify(item.Name), categorySlug, StringComparison.OrdinalIgnoreCase));
        return category is null ? null : PublicContentMapper.ToForumCategorySummary(category);
    }

    private void PopulatePage(ForumCategorySummary category)
    {
        Category = category;
        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Forum", "/forum"),
            new BreadcrumbItem(category.Name, category.DetailPath),
            new BreadcrumbItem("New thread", ForumRoutes.GetNewThreadPath(category)),
        ];
        ViewData["Title"] = $"New thread | {category.Name} | QueenZone forum";
        ViewData["CanonicalPath"] = ForumRoutes.GetNewThreadPath(category);
    }
}
