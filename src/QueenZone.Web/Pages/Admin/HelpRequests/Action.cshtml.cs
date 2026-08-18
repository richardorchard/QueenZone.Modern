using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.HelpRequests;

public sealed class ActionModel(IHelpRequestRepository helpRequestRepository) : AdminHelpRequestsPageModel
{
    [BindProperty]
    public string? Status { get; set; }

    [BindProperty]
    public string? ReviewNotes { get; set; }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Status))
        {
            return RedirectWithMessage(id, "A status is required.", "error");
        }

        try
        {
            var updated = await helpRequestRepository.UpdateStatusAsync(
                id,
                Status,
                EditorEmail,
                ReviewNotes,
                cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            TempData["HelpRequestMessage"] = $"Marked as {HelpRequestStatus.DisplayName(updated.Status)}.";
            TempData["HelpRequestMessageKind"] = "success";
        }
        catch (ArgumentException ex)
        {
            TempData["HelpRequestMessage"] = ex.Message;
            TempData["HelpRequestMessageKind"] = "error";
        }

        return Redirect($"/admin/help/{id}");
    }

    private IActionResult RedirectWithMessage(Guid id, string message, string kind)
    {
        TempData["HelpRequestMessage"] = message;
        TempData["HelpRequestMessageKind"] = kind;
        return Redirect($"/admin/help/{id}");
    }
}
