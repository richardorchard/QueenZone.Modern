using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectAuthoringAuthenticateScheme_with_member_cookie_uses_member_scheme(bool useAzureAd)
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAuthoringAuthenticateScheme(
            useAzureAd,
            hasMemberCookie: true);

        Assert.Equal(MemberAuthenticationSchemes.MembersCookie, scheme);
    }

    [Fact]
    public void SelectAuthoringAuthenticateScheme_without_member_cookie_uses_Entra_cookie()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAuthoringAuthenticateScheme(
            useAzureAd: true,
            hasMemberCookie: false);

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, scheme);
    }

    [Fact]
    public void SelectAuthoringAuthenticateScheme_without_member_cookie_or_Entra_uses_test_handler()
    {
        var scheme = QueenZoneAuthServiceCollectionExtensions.SelectAuthoringAuthenticateScheme(
            useAzureAd: false,
            hasMemberCookie: false);

        Assert.Equal(TestAuthHandler.SchemeName, scheme);
    }

    [Fact]
    public void Admin_policy_does_not_accept_member_cookie_or_mobile_bearer()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneAuthorization(configuration, new FakeHostEnvironment("Testing"));

        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value
            .GetPolicy(AdminAuthenticationSchemes.Policy);

        Assert.NotNull(policy);
        Assert.Equal([AdminAuthenticationSchemes.CompositeScheme], policy!.AuthenticationSchemes);
        Assert.DoesNotContain(MemberAuthenticationSchemes.MembersBearer, policy.AuthenticationSchemes);
        Assert.DoesNotContain(MemberAuthenticationSchemes.MembersCookie, policy.AuthenticationSchemes);
        Assert.DoesNotContain(TestMemberAuthHandler.SchemeName, policy.AuthenticationSchemes);
    }
}
