using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Submit;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class ArticleModel(
    IArticleSubmissionRepository articleSubmissionRepository,
    UgcHtml ugcHtml) : PageModel
{
    [BindProperty]
    public Guid? DraftId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(300, ErrorMessage = "Title must be 300 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500, ErrorMessage = "Excerpt must be 500 characters or fewer.")]
    public string? Excerpt { get; set; }

    [BindProperty]
    public string? Body { get; set; }

    [BindProperty]
    [StringLength(500, ErrorMessage = "Tags must be 500 characters or fewer.")]
    public string? Tags { get; set; }

    public string? StatusMessage { get; private set; }

    public string StatusMessageKind { get; private set; } = "success";

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (await GetCurrentMemberIdAsync() is null)
        {
            return Redirect("/account/login");
        }

        ViewData["Title"] = "Write an article";
        return Page();
    }

    public async Task<IActionResult> OnGetEditAsync(Guid id, CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        var submission = await articleSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null || submission.AuthorMemberId != memberId.Value)
        {
            return NotFound();
        }

        DraftId = submission.Id;
        Title = submission.Title;
        Excerpt = submission.Excerpt;
        Body = submission.Body;
        Tags = submission.Tags;

        ViewData["Title"] = "Edit draft";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? action, CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        ViewData["Title"] = "Write an article";

        if (action == "submit")
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var sanitizedBody = ugcHtml.Sanitize(Body);

            var draft = await articleSubmissionRepository.UpsertDraftAsync(
                new ArticleSubmissionDraft(DraftId, memberId.Value, Title, Excerpt, sanitizedBody, null, Tags),
                cancellationToken);

            try
            {
                var submitted = await articleSubmissionRepository.SubmitForReviewAsync(
                    draft.Id,
                    memberId.Value,
                    cancellationToken);

                if (submitted is null)
                {
                    StatusMessage = "Could not submit — the article may already be submitted.";
                    StatusMessageKind = "error";
                    return Page();
                }

                return Redirect($"/submit/article/confirmation/{submitted.Id:D}");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(Body), ex.Message);
                return Page();
            }
        }

        // Save draft
        if (string.IsNullOrWhiteSpace(Title))
        {
            ModelState.AddModelError(nameof(Title), "Title is required to save a draft.");
            return Page();
        }

        var savedBody = ugcHtml.Sanitize(Body);

        try
        {
            var saved = await articleSubmissionRepository.UpsertDraftAsync(
                new ArticleSubmissionDraft(DraftId, memberId.Value, Title, Excerpt, savedBody, null, Tags),
                cancellationToken);

            DraftId = saved.Id;
            StatusMessage = "Draft saved.";
            StatusMessageKind = "success";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = ex.Message;
            StatusMessageKind = "error";
        }

        return Page();
    }

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        var authResult = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return null;
        }

        var idValue = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }
}
