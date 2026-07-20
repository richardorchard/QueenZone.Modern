using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class DetailModel(
    IArticleSubmissionRepository articleSubmissionRepository,
    UgcHtml ugcHtml) : AdminArticlesPageModel
{
    public ArticleSubmission? Submission { get; private set; }

    public string FormattedBody { get; private set; } = string.Empty;

    public string? StatusMessage { get; private set; }

    public string StatusMessageKind { get; private set; } = "success";

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await articleSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null)
        {
            return NotFound();
        }

        Submission = submission;
        FormattedBody = ugcHtml.FormatForDisplay(submission.Body);

        StatusMessage = TempData["ArticleMessage"] as string;
        StatusMessageKind = TempData["ArticleMessageKind"] as string ?? "success";

        ViewData["Title"] = $"Review: {submission.Title}";
        return Page();
    }
}
