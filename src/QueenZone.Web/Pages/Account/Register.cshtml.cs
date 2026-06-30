using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Pages.Account;

public sealed class RegisterModel(
    MemberAccountService memberAccountService,
    IOptions<MemberAuthenticationOptions> memberAuthenticationOptions) : AccountPageModel(memberAuthenticationOptions)
{
    public string ReturnUrl { get; private set; } = "/";

    public string? Error { get; private set; }

    public void OnGet(string? returnUrl) => ReturnUrl = ResolveReturnUrl(returnUrl);

    public async Task<IActionResult> OnPostAsync(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string displayName,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        ReturnUrl = ResolveReturnUrl(returnUrl);

        var result = await memberAccountService.RegisterAsync(email, password, displayName, cancellationToken);
        if (!result.Succeeded)
        {
            Error = result.Error;
            return Page();
        }

        await SignInMemberAsync(result.Account!.Id, result.Account!.Email, result.Account!.DisplayName);
        return Redirect(ReturnUrl);
    }

    private Task SignInMemberAsync(Guid memberAccountId, string email, string displayName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, memberAccountId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName),
        };
        var identity = new ClaimsIdentity(claims, MemberAuthenticationSchemes.MembersCookie);
        return HttpContext.SignInAsync(MemberAuthenticationSchemes.MembersCookie, new ClaimsPrincipal(identity));
    }
}
