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

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [BindProperty]
    public bool AdoptLegacyDisplayName { get; set; } = true;

    /// <summary>
    /// Legacy USERS_T id chosen when claiming. Required when one or more free matches exist.
    /// </summary>
    [BindProperty]
    public int? SelectedLegacyUserId { get; set; }

    public string Email { get; private set; } = string.Empty;

    public Guid MemberId { get; private set; }

    public bool HasAvatar { get; private set; }

    public IReadOnlyList<string> LinkedProviders { get; private set; } = [];

    public LegacyAccountLinkState LegacyLink { get; private set; } = LegacyAccountLinkState.None();

    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        await PopulatePageAsync(account, cancellationToken);
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

        var account = await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        // Preserve form-bound DisplayName; only repopulate read-only fields.
        var submittedDisplayName = DisplayName?.Trim() ?? string.Empty;
        await PopulatePageAsync(account, cancellationToken);
        DisplayName = submittedDisplayName;
        ViewData["Title"] = "Account settings";

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

    public async Task<IActionResult> OnPostClaimLegacyAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        var account = await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        if (SelectedLegacyUserId is null)
        {
            await PopulatePageAsync(account, cancellationToken);
            ModelState.AddModelError(
                nameof(SelectedLegacyUserId),
                "Choose which legacy forum account to claim.");
            ViewData["Title"] = "Account settings";
            return Page();
        }

        var result = await memberAccountService.ClaimLegacyAccountAsync(
            memberId.Value,
            SelectedLegacyUserId.Value,
            AdoptLegacyDisplayName,
            cancellationToken);

        if (!result.Succeeded || result.Account is null)
        {
            await PopulatePageAsync(account, cancellationToken);
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not claim the legacy account.");
            ViewData["Title"] = "Account settings";
            return Page();
        }

        await ReissueMemberCookieAsync(result.Account);
        TempData[SuccessMessageKey] = AdoptLegacyDisplayName && !string.Equals(
                account.DisplayName,
                result.Account.DisplayName,
                StringComparison.Ordinal)
            ? "Legacy account claimed and display name updated."
            : "Legacy account claimed.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadAvatarAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        var account = await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
        if (account is null)
        {
            return Redirect("/account/login");
        }

        await PopulatePageAsync(account, cancellationToken);
        ViewData["Title"] = "Account settings";

        if (AvatarFile is null || AvatarFile.Length <= 0)
        {
            ModelState.AddModelError(nameof(AvatarFile), "Choose an image file to upload.");
            return Page();
        }

        await using var stream = AvatarFile.OpenReadStream();
        var result = await memberAccountService.UpdateAvatarAsync(
            memberId.Value,
            stream,
            AvatarFile.FileName,
            cancellationToken);

        if (!result.Succeeded || result.Account is null)
        {
            ModelState.AddModelError(nameof(AvatarFile), result.Error ?? "Could not update avatar.");
            return Page();
        }

        TempData[SuccessMessageKey] = "Avatar updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAvatarAsync(CancellationToken cancellationToken)
    {
        var memberId = await GetCurrentMemberIdAsync();
        if (memberId is null)
        {
            return Redirect("/account/login");
        }

        var result = await memberAccountService.RemoveAvatarAsync(memberId.Value, cancellationToken);
        if (!result.Succeeded)
        {
            var account = await memberAccountService.FindByIdAsync(memberId.Value, cancellationToken);
            if (account is null)
            {
                return Redirect("/account/login");
            }

            await PopulatePageAsync(account, cancellationToken);
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not remove avatar.");
            ViewData["Title"] = "Account settings";
            return Page();
        }

        TempData[SuccessMessageKey] = "Avatar removed.";
        return RedirectToPage();
    }

    private async Task PopulatePageAsync(Data.Entities.MemberAccount account, CancellationToken cancellationToken)
    {
        MemberId = account.Id;
        DisplayName = account.DisplayName;
        Email = account.Email;
        HasAvatar = !string.IsNullOrWhiteSpace(account.AvatarUrl);
        LinkedProviders = await memberAccountService.ListExternalProvidersAsync(account.Id, cancellationToken);
        LegacyLink = await memberAccountService.GetLegacyLinkStateAsync(account, cancellationToken);
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
