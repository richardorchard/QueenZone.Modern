using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Account;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class DeleteModel(MemberAccountService memberAccountService) : PageModel
{
    [BindProperty]
    public string Confirmation { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        Email = account.Email;
        ViewData["Title"] = "Delete account";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        Email = account.Email;
        ViewData["Title"] = "Delete account";
        if (!string.Equals(Confirmation?.Trim(), "DELETE", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(Confirmation), "Type DELETE to confirm account deletion.");
            return Page();
        }

        var result = await memberAccountService.RequestDeletionAsync(account.Id, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not request account deletion.");
            return Page();
        }

        await HttpContext.SignOutAsync(MemberAuthenticationSchemes.MembersCookie);
        return RedirectToPage("/Account/DeletionRequested");
    }

    private async Task<Data.Entities.MemberAccount?> LoadCurrentAccountAsync(CancellationToken cancellationToken)
    {
        var authResult = await HttpContext.AuthenticateMemberAsync();
        var idValue = authResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return authResult.Succeeded && Guid.TryParse(idValue, out var memberId)
            ? await memberAccountService.FindByIdAsync(memberId, cancellationToken)
            : null;
    }
}
