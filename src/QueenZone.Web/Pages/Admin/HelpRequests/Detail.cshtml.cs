using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.HelpRequests;

public sealed class DetailModel(IHelpRequestRepository helpRequestRepository) : AdminHelpRequestsPageModel
{
    public HelpRequest? Item { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Item = await helpRequestRepository.GetByIdAsync(id, cancellationToken);
        if (Item is null)
        {
            return NotFound();
        }

        StatusMessage = TempData["HelpRequestMessage"] as string;
        StatusMessageKind = TempData["HelpRequestMessageKind"] as string;
        ViewData["Title"] = $"Help request — {Item.Subject}";
        return Page();
    }
}
