using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Pages.Account;

public abstract class AccountPageModel(IOptions<MemberAuthenticationOptions> memberAuthenticationOptions) : PageModel
{
    public bool GoogleEnabled => memberAuthenticationOptions.Value.Google?.ClientId is { Length: > 0 };

    public bool MicrosoftEnabled => memberAuthenticationOptions.Value.Microsoft?.ClientId is { Length: > 0 };

    public bool FacebookEnabled => memberAuthenticationOptions.Value.Facebook?.ClientId is { Length: > 0 };

    protected static string ResolveReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? returnUrl
            : "/";
}
