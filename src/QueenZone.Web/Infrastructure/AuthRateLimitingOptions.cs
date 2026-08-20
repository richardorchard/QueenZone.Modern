namespace QueenZone.Web;

/// <summary>
/// In-process auth abuse limits shared by website login and <c>/api/v1/auth</c>.
/// Safe on a single B1 worker; not a distributed limiter.
/// </summary>
public sealed class AuthRateLimitingOptions
{
    public const string SectionName = "RateLimiting:Auth";

    /// <summary>Requests per client IP per window (web login + mobile auth).</summary>
    public int IpPermitLimit { get; set; } = 30;

    public int IpWindowMinutes { get; set; } = 1;

    /// <summary>Mobile sign-in completions and refresh grants per member account per window.</summary>
    public int AccountPermitLimit { get; set; } = 10;

    public int AccountWindowMinutes { get; set; } = 1;
}
