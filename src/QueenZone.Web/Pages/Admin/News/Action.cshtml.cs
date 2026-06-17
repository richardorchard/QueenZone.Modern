using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class ActionModel(
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository) : AdminNewsPageModel
{
    public async Task<IActionResult> OnPostPublishAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        await adminNewsRepository.PublishAsync(id, EditorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "publish", EditorEmail, $"Published \"{article.Title}\"", cancellationToken);
        return Redirect("/admin/news");
    }

    public async Task<IActionResult> OnPostUnpublishAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        await adminNewsRepository.UnpublishAsync(id, EditorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "unpublish", EditorEmail, $"Unpublished \"{article.Title}\"", cancellationToken);
        return Redirect("/admin/news");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        await auditRepository.AppendAsync(id, "delete", EditorEmail, $"Deleted \"{article.Title}\"", cancellationToken);
        await adminNewsRepository.DeleteAsync(id, EditorEmail, cancellationToken);
        return Redirect("/admin/news");
    }
}
