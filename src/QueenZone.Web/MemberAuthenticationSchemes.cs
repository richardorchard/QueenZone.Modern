namespace QueenZone.Web;

public static class MemberAuthenticationSchemes
{
    public const string MembersCookie = "MembersCookie";

    /// <summary>Short-lived cookie used only between an external provider's challenge and callback.</summary>
    public const string ExternalCookie = "MembersExternalCookie";

    public const string Google = "Google";

    public const string Microsoft = "Microsoft";

    public const string Facebook = "Facebook";

    public const string MemberPolicy = "Member";
}
