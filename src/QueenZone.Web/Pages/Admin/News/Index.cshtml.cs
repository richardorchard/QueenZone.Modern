using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class IndexModel(
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    UgcHtml ugcHtml) : AdminNewsListPageModel(adminNewsRepository)
{
    public ArticleFormViewModel? CreateForm { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken) =>
        await LoadListPageAsync(1, cancellationToken);

    public async Task<IActionResult> OnPostAsync([FromForm] AdminNewsForm form, CancellationToken cancellationToken)
    {
        var rawDraft = form.ToDraft();
        var draft = rawDraft with { Body = ugcHtml.Sanitize(rawDraft.Body) };
        var slugInUse = await AdminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(draft.Title, draft.Slug),
            cancellationToken: cancellationToken);
        var errors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Create news article";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        var id = await AdminNewsRepository.CreateDraftAsync(draft, EditorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "create", EditorEmail, $"Created draft \"{draft.Title}\"", cancellationToken);
        return Redirect($"/admin/news/{id}/edit");
    }
}
