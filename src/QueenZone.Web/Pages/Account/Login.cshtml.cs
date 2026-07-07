using Microsoft.Extensions.Options;

namespace QueenZone.Web.Pages.Account;

public sealed class LoginModel(IOptions<MemberAuthenticationOptions> memberAuthenticationOptions) : AccountPageModel(memberAuthenticationOptions)
{
    public string ReturnUrl { get; private set; } = "/";

    public bool ShowSignedOutMessage { get; private set; }

    public void OnGet(string? returnUrl, string? signedOut = null)
    {
        ReturnUrl = ResolveReturnUrl(returnUrl);
        ShowSignedOutMessage = string.Equals(signedOut, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(signedOut, "true", StringComparison.OrdinalIgnoreCase);
    }
}
