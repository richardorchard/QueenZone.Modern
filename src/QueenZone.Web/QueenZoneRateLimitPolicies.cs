namespace QueenZone.Web;

/// <summary>
/// Named ASP.NET Core rate-limit policies for abuse-sensitive routes.
/// Process-local (single B1 instance); partition by member id when signed in, else client IP.
/// </summary>
public static class QueenZoneRateLimitPolicies
{
    /// <summary>OAuth start / login challenges — IP partition.</summary>
    public const string Auth = "qz-auth";

    /// <summary>Member content submissions (news/photo/article forms).</summary>
    public const string MemberWrite = "qz-member-write";

    /// <summary>Editor image and avatar uploads.</summary>
    public const string Upload = "qz-upload";

    /// <summary>Public archive search (GET with query).</summary>
    public const string Search = "qz-search";
}
