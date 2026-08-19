using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Tests;

/// <summary>
/// Test double for Google/Microsoft/Discord/GitHub/Apple. Challenge immediately returns to
/// the mobile callback so PKCE tests do not need a live identity provider.
/// </summary>
internal sealed class TestOAuthProviderHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect(properties.RedirectUri ?? "/");
        return Task.CompletedTask;
    }
}
