using Microsoft.Extensions.Options;

namespace QueenZone.Web.Pages.Account;

public sealed class LoginModel(IOptions<MemberAuthenticationOptions> memberAuthenticationOptions) : AccountPageModel(memberAuthenticationOptions)
{
    public string ReturnUrl { get; private set; } = "/";

    public void OnGet(string? returnUrl) => ReturnUrl = ResolveReturnUrl(returnUrl);
}
