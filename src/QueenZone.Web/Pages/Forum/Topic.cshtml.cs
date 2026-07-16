using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Pages.Forum;

[RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
[RequestSizeLimit(55 * 1024 * 1024)]
public sealed class TopicModel : ForumTopicPageModel
{
    private readonly IForumRepository forumRepository;
    private readonly IForumWriteRepository forumWriteRepository;
    private readonly IForumPollRepository forumPollRepository;
    private readonly MemberAccountService memberAccountService;
    private readonly PublicQueryCacheService publicQueryCache;
    private readonly UgcHtml ugcHtml;
    private readonly ForumPostRateLimiter rateLimiter;
    private readonly ForumAttachmentValidator attachmentValidator;
    private readonly ForumAttachmentUploadService attachmentUploadService;
    private readonly AdminOptions adminOptions;
    private readonly TimeProvider timeProvider;

    public TopicModel(
        IForumRepository forumRepository,
        IForumWriteRepository forumWriteRepository,
        IForumPollRepository forumPollRepository,
        MemberAccountService memberAccountService,
        PublicQueryCacheService publicQueryCache,
        UgcHtml ugcHtml,
        ForumPostRateLimiter rateLimiter,
        ForumAttachmentValidator attachmentValidator,
        ForumAttachmentUploadService attachmentUploadService,
        IOptions<AdminOptions> adminOptions,
        IOptions<ForumOptions> forumOptions,
        TimeProvider timeProvider)
        : base(forumRepository, forumOptions, adminOptions, timeProvider)
    {
        this.forumRepository = forumRepository;
        this.forumWriteRepository = forumWriteRepository;
        this.forumPollRepository = forumPollRepository;
        this.memberAccountService = memberAccountService;
        this.publicQueryCache = publicQueryCache;
        this.ugcHtml = ugcHtml;
        this.rateLimiter = rateLimiter;
        this.attachmentValidator = attachmentValidator;
        this.attachmentUploadService = attachmentUploadService;
        this.adminOptions = adminOptions.Value;
        this.timeProvider = timeProvider;
    }

    [BindProperty]
    [Required]
    public string Body { get; set; } = string.Empty;

    [BindProperty]
    public List<IFormFile> Attachments { get; set; } = [];

    public bool CanReply { get; private set; }

    public ForumPollResults? Poll { get; private set; }

    public async Task<IActionResult> OnGetAsync(int topicId, string slug, CancellationToken cancellationToken)
    {
        var result = await LoadTopicPageAsync(topicId, slug, 1, cancellationToken);
        CanReply = await GetCurrentMemberIdAsync() is not null;
        if (result is PageResult)
        {
            await LoadPollAsync(topicId, cancellationToken);
        }

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
        await LoadPollAsync(topicId, cancellationToken);

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
        int postId;
        try
        {
            postId = await forumWriteRepository.CreatePostAsync(
                new NewForumPost(
                    topicId,
                    memberId.Value,
                    authorDisplayName,
                    sanitizedBody,
                    timeProvider.GetUtcNow()),
                cancellationToken);

            if (attachmentValidation.AcceptedFiles.Count > 0)
            {
                await attachmentUploadService.UploadAndSaveAsync(
                    postId,
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
        var updatedPage = await forumRepository.GetTopicPostsPageAsync(topicId, 1, 1, cancellationToken);
        var lastPage = updatedPage is null
            ? 1
            : Math.Max(1, ForumRoutes.GetPostsTotalPages(updatedPage.TotalCount, ForumRoutes.PostsPageSize));
        var redirectPath = Header is null
            ? ForumRoutes.GetTopicCanonicalPath(topicId, slug, lastPage)
            : ForumRoutes.GetTopicCanonicalPath(Header, lastPage);

        return Redirect(redirectPath + $"#post-{postId}");
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

    private async Task LoadPollAsync(int topicId, CancellationToken cancellationToken)
    {
        var memberAuth = await ResolveMemberAuthAsync();
        var memberId = ForumMember.GetMemberId(memberAuth?.Principal);
        var isAdmin = memberAuth?.Principal is not null
            && ForumPollEndpoints.IsAdmin(memberAuth.Principal, adminOptions);
        Poll = await forumPollRepository.GetPollWithResultsAsync(
            topicId,
            memberId,
            isAdmin,
            cancellationToken);
    }
}
