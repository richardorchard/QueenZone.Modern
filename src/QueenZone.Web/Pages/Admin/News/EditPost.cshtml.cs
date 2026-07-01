using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class EditPostModel(
    IAdminNewsRepository adminNewsRepository,
    INewsDiscoveryRepository discoveryRepository,
    INewsAuditRepository auditRepository) : AdminNewsPageModel
{
    public ArticleFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnPostAsync(
        int id,
        [FromForm] AdminNewsForm form,
        CancellationToken cancellationToken)
    {
        var existing = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        var draft = form.ToDraft();
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(draft.Title, draft.Slug),
            excludeNewsId: id,
            cancellationToken: cancellationToken);
        var errors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (errors.Count > 0)
        {
            var provenance = await NewsDiscoveryProvenanceBuilder.LoadForPromotedArticleAsync(
                discoveryRepository,
                id,
                cancellationToken);
            ViewData["Title"] = "Edit article";
            Form = EditModel.BuildForm(existing, draft, errors, provenance);
            return Page();
        }

        await adminNewsRepository.UpdateAsync(id, draft, EditorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "edit", EditorEmail, $"Updated \"{draft.Title}\"", cancellationToken);
        return Redirect($"/admin/news/{id}/edit");
    }
}
