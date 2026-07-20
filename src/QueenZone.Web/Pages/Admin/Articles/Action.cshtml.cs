using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class ActionModel(IArticleSubmissionRepository articleSubmissionRepository) : AdminArticlesPageModel
{
    [BindProperty]
    public string? Slug { get; set; }

    [BindProperty]
    public string? Excerpt { get; set; }

    [BindProperty]
    public string? Tags { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    public async Task<IActionResult> OnPostAsync(Guid id, string submitAction, CancellationToken cancellationToken)
    {
        return submitAction switch
        {
            "approve" => await ApplyAsync(id, ArticleSubmissionStatus.ApprovedForPublishing,
                rejectionReason: null, "Approved for publishing.", cancellationToken),
            "publish" => await ApplyAsync(id, ArticleSubmissionStatus.Published,
                rejectionReason: null, "Article published.", cancellationToken),
            "revise" => await ApplyRevisionRequestAsync(id, cancellationToken),
            "reject" => await ApplyRejectAsync(id, cancellationToken),
            "underreview" => await ApplyAsync(id, ArticleSubmissionStatus.UnderReview,
                rejectionReason: null, "Marked under review.", cancellationToken),
            _ => Redirect($"/admin/articles/{id}"),
        };
    }

    private async Task<IActionResult> ApplyAsync(
        Guid id,
        string status,
        string? rejectionReason,
        string successMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await articleSubmissionRepository.UpdateStatusAsync(
                id,
                status,
                EditorEmail,
                ReviewNotes,
                rejectionReason,
                slug: Slug,
                excerpt: Excerpt,
                tags: Tags,
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            TempData["ArticleMessage"] = successMessage;
            TempData["ArticleMessageKind"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ArticleMessage"] = ex.Message;
            TempData["ArticleMessageKind"] = "error";
        }

        return Redirect($"/admin/articles/{id}");
    }

    private async Task<IActionResult> ApplyRevisionRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        return await ApplyAsync(id, ArticleSubmissionStatus.RequiresRevision,
            rejectionReason: RejectionReason, "Revision requested.", cancellationToken);
    }

    private async Task<IActionResult> ApplyRejectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(RejectionReason))
        {
            TempData["ArticleMessage"] = "A rejection reason is required.";
            TempData["ArticleMessageKind"] = "error";
            return Redirect($"/admin/articles/{id}");
        }

        return await ApplyAsync(id, ArticleSubmissionStatus.Rejected,
            rejectionReason: RejectionReason, "Article rejected.", cancellationToken);
    }
}
