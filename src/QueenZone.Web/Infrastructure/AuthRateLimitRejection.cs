using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace QueenZone.Web;

/// <summary>
/// Writes rate-limit rejections and logs the route + client IP only (never tokens,
/// Authorization headers, or query strings).
/// </summary>
public static class AuthRateLimitRejection
{
    public const string OauthError = "temporarily_unavailable";

    public const string OauthDescription = "Too many attempts. Try again later.";

    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        var logger = http.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("QueenZone.Web.RateLimiting");

        logger.LogWarning(
            "Rate limit rejected {Method} {Path} from {ClientIp}",
            ApiV1ErrorHandling.SanitizeHttpMethodForLog(http.Request.Method),
            ApiV1ErrorHandling.SanitizeForLog(http.Request.Path.Value),
            http.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        if (http.Response.HasStarted)
        {
            return;
        }

        http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        if (!ApiV1.IsApiPath(http.Request.Path))
        {
            return;
        }

        if (IsOauthAuthPath(http.Request.Path))
        {
            await Results.Json(
                    new { error = OauthError, error_description = OauthDescription },
                    statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(http);
            return;
        }

        await Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests")
            .ExecuteAsync(http);
    }

    internal static bool IsOauthAuthPath(PathString path) =>
        path.StartsWithSegments(MobileAuthEndpoints.AuthorizePath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(MobileAuthEndpoints.CallbackPath, StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments(MobileAuthEndpoints.TokenPath, StringComparison.OrdinalIgnoreCase);
}
