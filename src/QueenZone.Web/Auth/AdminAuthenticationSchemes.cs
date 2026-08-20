namespace QueenZone.Web;

public static class AdminAuthenticationSchemes
{
    public const string CompositeScheme = "AdminAccess";

    public const string AuthoringCompositeScheme = "AuthoringAccess";

    /// <summary>
    /// Authorization policy for website admin pages and any admin-capable API route.
    /// Authenticates only <see cref="CompositeScheme"/> (Entra cookie or test admin header)
    /// and then re-checks <c>Admin:AllowedEmails</c>. Member cookies and mobile JWTs are
    /// never authentication schemes for this policy.
    /// </summary>
    public const string Policy = "Admin";

    public const string MemberCookieName = ".AspNetCore." + MemberAuthenticationSchemes.MembersCookie;
}
