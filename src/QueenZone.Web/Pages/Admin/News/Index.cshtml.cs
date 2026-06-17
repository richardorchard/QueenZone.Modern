using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class IndexModel(
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository) : AdminNewsPageModel
{
    public IReadOnlyList<AdminNewsArticle> Articles { get; private set; } = [];

    public ArticleFormViewModel? CreateForm { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Articles = await adminNewsRepository.GetAllAsync(cancellationToken);
        ViewData["Title"] = "Admin news";
    }

    public async Task<IActionResult> OnPostAsync([FromForm] AdminNewsForm form, CancellationToken cancellationToken)
    {
        var draft = form.ToDraft();
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(draft.Title, draft.Slug),
            cancellationToken: cancellationToken);
        var errors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (errors.Count > 0)
        {
            ViewData["Title"] = "Create news article";
            CreateForm = NewModel.BuildForm(draft, errors);
            return Page();
        }

        var id = await adminNewsRepository.CreateDraftAsync(draft, EditorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "create", EditorEmail, $"Created draft \"{draft.Title}\"", cancellationToken);
        return Redirect($"/admin/news/{id}/edit");
    }
}
