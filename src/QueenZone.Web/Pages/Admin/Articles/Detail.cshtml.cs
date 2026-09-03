using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class DetailModel(
    IArticleSubmissionRepository articleSubmissionRepository,
    IEditorialArticleRepository editorialArticles,
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

    public async Task<IActionResult> OnPostPrepareAsync(Guid id, CancellationToken cancellationToken)
    {
        var submission = await articleSubmissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission is null) return NotFound();
        var existing = (await editorialArticles.GetAllAsync(cancellationToken)).SingleOrDefault(x => x.SourceSubmissionId == id);
        if (existing is not null) return Redirect($"/admin/articles/editor/{existing.Id}");
        var saved = await editorialArticles.SaveDraftAsync(new EditorialArticleDraft(
            null, null, submission.Id, submission.Title, submission.Slug, submission.Excerpt ?? string.Empty,
            submission.Body, submission.AuthorDisplayName ?? "QueenZone contributor", "Feature", submission.Tags, null,
            submission.CoverImageBlobPath, submission.PublishedAt ?? DateTimeOffset.UtcNow), EditorEmail, cancellationToken);
        return Redirect($"/admin/articles/editor/{saved.Id}");
    }
}
