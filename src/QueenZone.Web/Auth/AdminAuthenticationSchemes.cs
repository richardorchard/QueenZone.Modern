namespace QueenZone.Web;

public static class AdminAuthenticationSchemes
{
    public const string CompositeScheme = "AdminAccess";

    public const string MemberCookieName = ".AspNetCore." + MemberAuthenticationSchemes.MembersCookie;
}
