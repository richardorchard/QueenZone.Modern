using System.Security.Claims;

namespace QueenZone.Web.Tests;

public sealed class AdminAllowlistTests
{
    [Fact]
    public void IsAllowed_matches_email_claim_case_insensitively()
    {
        var user = Principal(new Claim(ClaimTypes.Email, "Admin@Example.com"));
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };

        Assert.True(AdminAllowlist.IsAllowed(user, options));
    }

    [Fact]
    public void IsAllowed_matches_preferred_username_when_email_claim_is_missing()
    {
        var user = Principal(new Claim("preferred_username", "admin@example.com"));
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };

        Assert.True(AdminAllowlist.IsAllowed(user, options));
    }

    [Fact]
    public void IsAllowed_matches_identity_name_when_other_claims_are_missing()
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(identity.NameClaimType, "admin@example.com"));
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };

        Assert.True(AdminAllowlist.IsAllowed(new ClaimsPrincipal(identity), options));
        Assert.Equal("admin@example.com", AdminAllowlist.ResolveEmail(new ClaimsPrincipal(identity)));
    }

    [Fact]
    public void IsAllowed_rejects_unknown_email_and_empty_principals()
    {
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };

        Assert.False(AdminAllowlist.IsAllowed(Principal(new Claim(ClaimTypes.Email, "fan@example.com")), options));
        Assert.False(AdminAllowlist.IsAllowed(new ClaimsPrincipal(), options));
        Assert.False(AdminAllowlist.IsAllowed(null, options));
        Assert.False(AdminAllowlist.IsAllowed(Principal(new Claim(ClaimTypes.Email, "admin@example.com")), allowedEmails: null));
        Assert.False(AdminAllowlist.IsAllowed(Principal(new Claim(ClaimTypes.Email, "admin@example.com")), new AdminOptions { AllowedEmails = ["  "] }));
    }

    [Fact]
    public void Forum_poll_admin_check_uses_the_same_allowlist()
    {
        var options = new AdminOptions { AllowedEmails = ["admin@example.com"] };
        var user = Principal(new Claim(ClaimTypes.Email, "admin@example.com"));

        Assert.True(ForumPollEndpoints.IsAdmin(user, options));
        Assert.False(ForumPollEndpoints.IsAdmin(new ClaimsPrincipal(new ClaimsIdentity()), options));
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));
}
