using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Pages.Account;

public sealed class SignInModel(
    MemberAccountService memberAccountService,
    IOptions<MemberAuthenticationOptions> memberAuthenticationOptions) : AccountPageModel(memberAuthenticationOptions)
{
    public string ReturnUrl { get; private set; } = "/";

    public string? Error { get; private set; }

    public void OnGet(string? returnUrl) => ReturnUrl = ResolveReturnUrl(returnUrl);

    public async Task<IActionResult> OnPostAsync(
        [FromForm] string email,
        [FromForm] string password,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        ReturnUrl = ResolveReturnUrl(returnUrl);

        var result = await memberAccountService.SignInAsync(email, password, cancellationToken);
        if (!result.Succeeded)
        {
            Error = result.Error;
            return Page();
        }

        var account = result.Account!;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Name, account.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, MemberAuthenticationSchemes.MembersCookie);
        await HttpContext.SignInAsync(MemberAuthenticationSchemes.MembersCookie, new ClaimsPrincipal(identity));

        return Redirect(ReturnUrl);
    }
}
