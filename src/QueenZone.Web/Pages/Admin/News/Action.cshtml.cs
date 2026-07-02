using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class ActionModel(
    IAdminNewsRepository adminNewsRepository,
    INewsAuditRepository auditRepository,
    INewsDiscoveryRepository discoveryRepository) : AdminNewsPageModel
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

        try
        {
            await discoveryRepository.ClearPromotedNewsLinksAsync(id, cancellationToken);
        }
        catch (Exception)
        {
            // Best-effort cleanup when discovery tables are unavailable or unmigrated.
        }

        try
        {
            await auditRepository.AppendAsync(id, "delete", EditorEmail, $"Deleted \"{article.Title}\"", cancellationToken);
            await adminNewsRepository.DeleteAsync(id, EditorEmail, cancellationToken);
        }
        catch (DbUpdateException ex) when (AdminNewsDeleteError.IsForeignKeyViolation(ex))
        {
            TempData[AdminNewsMessages.MessageKey] =
                "This article could not be deleted because other archive records still reference it. Unpublish it instead to hide it from the public site.";
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect("/admin/news");
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            TempData[AdminNewsMessages.MessageKey] =
                "This article could not be deleted because other archive records still reference it. Unpublish it instead to hide it from the public site.";
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect("/admin/news");
        }
        catch (InvalidOperationException ex)
        {
            TempData[AdminNewsMessages.MessageKey] = ex.Message;
            TempData[AdminNewsMessages.MessageKindKey] = "error";
            return Redirect("/admin/news");
        }

        return Redirect("/admin/news");
    }
}
