using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QueenZone.Web.Pages.Account;

[Authorize(Policy = MemberAuthenticationSchemes.MemberPolicy, AuthenticationSchemes = MemberAuthenticationSchemes.MembersCookie)]
public sealed class SettingsModel(MemberAccountService memberAccountService) : PageModel
{
    public const string SuccessMessageKey = "AccountSettingsSuccess";

    [BindProperty]
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(
        MemberAccountService.MaxDisplayNameLength,
        MinimumLength = MemberAccountService.MinDisplayNameLength,
        ErrorMessage = "Display name must be between {2} and {1} characters.")]
    [Display(Name = "Display name")]
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public IReadOnlyList<string> LinkedProviders { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        DisplayName = account.DisplayName;
        Email = account.Email;
        LinkedProviders = await memberAccountService.ListExternalProvidersAsync(account.Id, cancellationToken);
        StatusMessage = TempData[SuccessMessageKey] as string;
        ViewData["Title"] = "Account settings";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        // Always repopulate read-only fields so validation failures keep the page usable.
        var account = await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        Email = account.Email;
        LinkedProviders = await memberAccountService.ListExternalProvidersAsync(account.Id, cancellationToken);
        ViewData["Title"] = "Account settings";

        // Strip whitespace before model validation so min/max lengths apply to the trimmed value.
        DisplayName = DisplayName?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await memberAccountService.UpdateDisplayNameAsync(memberId.Value, DisplayName, cancellationToken);
        if (!result.Succeeded || result.Account is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update display name.");
            return Page();
        }

        await ReissueMemberCookieAsync(result.Account);

        TempData[SuccessMessageKey] = "Display name updated.";
        return RedirectToPage();
    }

    private async Task<Data.Entities.MemberAccount?> LoadCurrentAccountAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return null;
        }

        return await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
    }

    private async Task<Guid?> GetCurrentMemberIdAsync()
    {
        // Authenticate the member cookie explicitly — ambient User may be the admin scheme.
        var authResult = await HttpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (!authResult.Succeeded || authResult.Principal is null)
        {
            return null;
        }

        var idValue = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idValue, out var id) ? id : null;
    }

    private async Task ReissueMemberCookieAsync(Data.Entities.MemberAccount account)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Name, account.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, MemberAuthenticationSchemes.MembersCookie);
        await HttpContext.SignInAsync(MemberAuthenticationSchemes.MembersCookie, new ClaimsPrincipal(identity));
    }
}
