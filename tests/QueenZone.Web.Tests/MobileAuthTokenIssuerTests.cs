using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthTokenIssuerTests
{
    [Fact]
    public void IssueAccessToken_IsValidJwtForConfiguredAudience()
    {
        var options = Options.Create(new MobileAuthOptions());
        var site = Options.Create(new SiteOptions { PublicBaseUrl = "https://www.queenzone.org" });
        var issuer = new MobileAuthTokenIssuer(
            options,
            site,
            new FakeHostEnvironment("Testing"),
            TimeProvider.System);
        var memberId = Guid.NewGuid();

        var token = issuer.IssueAccessToken(memberId, "fan@example.com", "Fan");

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token,
            MobileAuthTokenIssuer.CreateValidationParameters(
                issuer.Issuer,
                issuer.Audience,
                MobileAuthOptions.DevelopmentSigningKey),
            out var validated);
        Assert.Equal(memberId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("fan@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal("Fan", principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(issuer.Issuer, validated.Issuer);
        Assert.True(issuer.AccessTokenLifetimeSeconds is >= 60 and <= 120 * 60);
    }
}
