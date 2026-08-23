using System.Security.Cryptography;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthPkceTests
{
    [Fact]
    public void CreateS256Challenge_MatchesRfc7636Example()
    {
        // RFC 7636 appendix B.
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", MobileAuthPkce.CreateS256Challenge(verifier));
        Assert.True(MobileAuthPkce.VerifyS256(verifier, "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("has space characters that are not allowed in pkce!!!!")]
    public void IsValidCodeVerifier_RejectsInvalidValues(string? verifier)
    {
        Assert.False(MobileAuthPkce.IsValidCodeVerifier(verifier));
    }

    [Fact]
    public void IsValidCodeVerifier_AcceptsUnreserved43To128()
    {
        var min = new string('a', 43);
        var max = new string('Z', 128)[..128];
        Assert.True(MobileAuthPkce.IsValidCodeVerifier(min));
        Assert.True(MobileAuthPkce.IsValidCodeVerifier(max));
        Assert.False(MobileAuthPkce.IsValidCodeVerifier(new string('a', 42)));
        Assert.False(MobileAuthPkce.IsValidCodeVerifier(new string('a', 129)));
    }

    [Fact]
    public void VerifyS256_RejectsWrongVerifier()
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        var lastChar = pair.Verifier[^1];
        var replacement = lastChar == 'A' ? 'B' : 'A';
        Assert.False(MobileAuthPkce.VerifyS256(pair.Verifier[..^1] + replacement, pair.Challenge));
    }

    [Fact]
    public void Sha256Hex_IsStableAndUppercase()
    {
        Assert.Equal(64, MobileAuthPkce.Sha256Hex("abc").Length);
        Assert.Equal(MobileAuthPkce.Sha256Hex("abc"), MobileAuthPkce.Sha256Hex("abc"));
        Assert.NotEqual(MobileAuthPkce.Sha256Hex("abc"), MobileAuthPkce.Sha256Hex("abd"));
    }

    [Fact]
    public void CreateOpaqueToken_IsUrlSafeAndUnique()
    {
        var first = MobileAuthPkce.CreateOpaqueToken();
        var second = MobileAuthPkce.CreateOpaqueToken();
        Assert.NotEqual(first, second);
        Assert.True(MobileAuthPkce.IsValidCodeVerifier(first));
        Assert.DoesNotContain('+', first);
        Assert.DoesNotContain('/', first);
        Assert.DoesNotContain('=', first);
    }

    [Fact]
    public void ToBase64Url_StripsPadding()
    {
        var encoded = MobileAuthPkce.ToBase64Url(RandomNumberGenerator.GetBytes(32));
        Assert.Equal(43, encoded.Length);
    }
}

internal static class MobileAuthPkceTestData
{
    public const string RedirectUri = "queenzone://auth/callback";

    public static (string Verifier, string Challenge) CreatePair()
    {
        var verifier = MobileAuthPkce.ToBase64Url(RandomNumberGenerator.GetBytes(32));
        return (verifier, MobileAuthPkce.CreateS256Challenge(verifier));
    }
}
