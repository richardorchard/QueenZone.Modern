using Microsoft.AspNetCore.Authentication;

namespace QueenZone.Web;

public static class HttpContextMemberAuthExtensions
{
    /// <summary>
    /// Authenticates the current request against the member cookie, falling back to
    /// <see cref="TestMemberAuthHandler"/> on automated-test hosts (<see
    /// cref="QueenZoneEnvironments.UsesTestAuth"/>) when no member cookie is present, since
    /// WebApplicationFactory and Playwright cannot complete a real external-provider login.
    /// </summary>
    public static async Task<AuthenticateResult> AuthenticateMemberAsync(this HttpContext httpContext)
    {
        var memberCookie = await httpContext.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (memberCookie.Succeeded)
        {
            return memberCookie;
        }

        var environment = httpContext.RequestServices.GetService<IHostEnvironment>();
        if (environment is not null && QueenZoneEnvironments.UsesTestAuth(environment))
        {
            var testMember = await httpContext.AuthenticateAsync(TestMemberAuthHandler.SchemeName);
            if (testMember.Succeeded)
            {
                return testMember;
            }
        }

        return memberCookie;
    }
}
