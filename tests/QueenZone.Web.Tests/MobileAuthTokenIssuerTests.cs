using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
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
        Assert.True(issuer.CanIssueTokens);
        Assert.True(validated.ValidTo <= DateTime.UtcNow.AddMinutes(15).AddSeconds(30));
        Assert.True(validated.ValidTo > DateTime.UtcNow.AddMinutes(10));
    }

    [Fact]
    public void IssueAccessToken_ThrowsInProductionWithoutSigningKey()
    {
        var issuer = new MobileAuthTokenIssuer(
            Options.Create(new MobileAuthOptions()),
            Options.Create(new SiteOptions { PublicBaseUrl = "https://www.queenzone.org" }),
            new FakeHostEnvironment("Production"),
            TimeProvider.System);

        Assert.False(issuer.CanIssueTokens);
        var ex = Assert.Throws<InvalidOperationException>(
            () => issuer.IssueAccessToken(Guid.NewGuid(), "fan@example.com", "Fan"));
        Assert.Contains("MobileAuth:SigningKey", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureJwtBearer_DoesNotUseDevelopmentKey_WhenProductionSigningKeyMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Site:PublicBaseUrl"] = "https://www.queenzone.org",
            })
            .Build();
        var jwt = new JwtBearerOptions();

        MobileAuthTokenIssuer.ConfigureJwtBearer(jwt, configuration, new FakeHostEnvironment("Production"));

        var key = Assert.IsType<SymmetricSecurityKey>(jwt.TokenValidationParameters.IssuerSigningKey);
        var material = System.Text.Encoding.UTF8.GetString(key.Key);
        Assert.NotEqual(MobileAuthOptions.DevelopmentSigningKey, material);
        Assert.True(material.Length >= 32);
    }

    [Fact]
    public void ResolveJwtValidationSigningKey_UsesDevelopmentKey_OutsideProduction()
    {
        var key = MobileAuthTokenIssuer.ResolveJwtValidationSigningKey(
            new MobileAuthOptions(),
            new FakeHostEnvironment("Testing"));
        Assert.Equal(MobileAuthOptions.DevelopmentSigningKey, key);
    }

    [Fact]
    public void ResolveJwtValidationSigningKey_UsesConfiguredProductionKey()
    {
        var key = MobileAuthTokenIssuer.ResolveJwtValidationSigningKey(
            new MobileAuthOptions { SigningKey = "production-mobile-auth-signing-key!!" },
            new FakeHostEnvironment("Production"));
        Assert.Equal("production-mobile-auth-signing-key!!", key);
    }

    [Fact]
    public void CreateValidationParameters_RejectsExpiredAccessToken()
    {
        var parameters = MobileAuthTokenIssuer.CreateValidationParameters(
            "https://www.queenzone.org",
            MobileAuthOptions.DefaultClientId,
            MobileAuthOptions.DevelopmentSigningKey);
        parameters.ClockSkew = TimeSpan.Zero;
        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(MobileAuthOptions.DevelopmentSigningKey));
        var expired = new JwtSecurityToken(
            issuer: "https://www.queenzone.org",
            audience: MobileAuthOptions.DefaultClientId,
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: now.AddHours(-2),
            expires: now.AddMinutes(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var jwt = new JwtSecurityTokenHandler().WriteToken(expired);

        var ex = Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(jwt, parameters, out _));
        Assert.DoesNotContain("refresh", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
