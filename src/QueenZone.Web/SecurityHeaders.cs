namespace QueenZone.Web;

/// <summary>
/// Applies baseline HTTP security headers for all responses.
/// CSP starts as Report-Only so production can inventory violations before enforce mode.
/// </summary>
public static class SecurityHeaders
{
    /// <summary>
    /// Report-only CSP for first rollout. Tighten to enforce after reviewing reports
    /// (Quill is self-hosted; analytics uses Google Tag Manager / gtag).
    /// </summary>
    public const string ContentSecurityPolicyReportOnly =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "img-src 'self' data: blob: https://cdn.queenzone.org https://cdn2.queenzone.org https://*.blob.core.windows.net; " +
        "font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline' https://www.googletagmanager.com https://www.google-analytics.com; " +
        "connect-src 'self' https://www.google-analytics.com https://*.google-analytics.com https://*.analytics.google.com https://www.googletagmanager.com; " +
        "frame-src 'self' https://www.googletagmanager.com; " +
        "object-src 'none'";

    public const string PermissionsPolicy = "camera=(), microphone=(), geolocation=()";

    public static void Apply(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = PermissionsPolicy;
        headers["Content-Security-Policy-Report-Only"] = ContentSecurityPolicyReportOnly;
    }
}
