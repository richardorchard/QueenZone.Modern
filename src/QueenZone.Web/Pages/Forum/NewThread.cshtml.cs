using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Pages.Forum;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy)]
[RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
[RequestSizeLimit(55 * 1024 * 1024)]
public sealed class NewThreadModel(
    IForumRepository forumRepository,
    IForumWriteRepository forumWriteRepository,
    MemberAccountService memberAccountService,
    PublicQueryCacheService publicQueryCache,
    UgcHtml ugcHtml,
    ForumPostRateLimiter rateLimiter,
    ForumAttachmentValidator attachmentValidator,
    ForumAttachmentUploadService attachmentUploadService,
    TimeProvider timeProvider) : PageModel
{
    [BindProperty]
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Subject { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    public string Body { get; set; } = string.Empty;

    [BindProperty]
    public List<IFormFile> Attachments { get; set; } = [];

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

        var selectedFiles = Attachments?.Where(file => file is { Length: > 0 }).ToList() ?? [];
        var attachmentValidation = attachmentValidator.Validate(selectedFiles);
        foreach (var error in attachmentValidation.Errors)
        {
            ModelState.AddModelError(nameof(Attachments), error);
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

        var authorDisplayName = await ResolveAuthorDisplayNameAsync(memberId.Value, cancellationToken);
        ForumThreadCreateResult created;
        try
        {
            created = await forumWriteRepository.CreateThreadAsync(
                new NewForumThread(
                    category.Id,
                    memberId.Value,
                    authorDisplayName,
                    Subject,
                    sanitizedBody,
                    timeProvider.GetUtcNow()),
                cancellationToken);

            if (attachmentValidation.AcceptedFiles.Count > 0)
            {
                await attachmentUploadService.UploadAndSaveAsync(
                    created.StarterPostId,
                    memberId.Value,
                    attachmentValidation.AcceptedFiles,
                    cancellationToken);
            }
        }
        catch (NotSupportedException ex)
        {
            ModelState.AddModelError(nameof(Attachments), ex.Message);
            Body = sanitizedBody;
            return Page();
        }
        catch (BlobUploadException ex)
        {
            ModelState.AddModelError(nameof(Attachments), ex.Message);
            Body = sanitizedBody;
            return Page();
        }

        publicQueryCache.InvalidateForumStatsCache();
        return Redirect(ForumRoutes.GetTopicCanonicalPath(created.TopicId, Subject));
    }

    private async Task<ForumCategorySummary?> ResolveCategoryAsync(string categorySlug, CancellationToken cancellationToken)
    {
        var categories = await forumRepository.GetCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(item =>
            string.Equals(NewsSlug.Slugify(item.Name), categorySlug, StringComparison.OrdinalIgnoreCase));
        return category is null ? null : PublicContentMapper.ToForumCategorySummary(category);
    }

    private async Task<string> ResolveAuthorDisplayNameAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var account = await memberAccountService.FindByIdAsync(memberId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(account?.DisplayName))
        {
            return account.DisplayName;
        }

        return string.IsNullOrWhiteSpace(User.Identity?.Name) ? "Member" : User.Identity.Name;
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
