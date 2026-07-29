using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace QueenZone.Web.Tests;

public sealed class AdminAuthenticationSchemeTests
{
    [Fact]
    public void SelectAdminChallengeScheme_with_Entra_uses_OpenIdConnect()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAdminChallengeScheme(useAzureAd: true);

        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, scheme);
        Assert.NotEqual(MemberAuthenticationSchemes.MembersCookie, scheme);
    }

    [Fact]
    public void SelectAdminChallengeScheme_without_Entra_uses_test_handler()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAdminChallengeScheme(useAzureAd: false);

        Assert.Equal(TestAuthHandler.SchemeName, scheme);
    }

    [Fact]
    public void SelectAdminAuthenticateScheme_with_Entra_uses_cookie_for_existing_session()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAdminAuthenticateScheme(useAzureAd: true);

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, scheme);
    }

    [Fact]
    public void SelectAdminAuthenticateScheme_without_Entra_uses_test_handler()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAdminAuthenticateScheme(useAzureAd: false);

        Assert.Equal(TestAuthHandler.SchemeName, scheme);
    }
}
