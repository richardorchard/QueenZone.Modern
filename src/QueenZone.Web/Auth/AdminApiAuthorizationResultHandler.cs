using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.WebUtilities;

namespace QueenZone.Web;

/// <summary>
/// Admin API routes use the same <see cref="AdminAuthenticationSchemes.Policy"/> as Razor
/// <c>/admin</c> pages, but JSON clients must not be redirected to Entra (or given an empty
/// 401). Member JWT challenges stay on the default handler so <c>WWW-Authenticate: Bearer</c>
/// is preserved on <c>/api/v1/auth/session</c>.
/// </summary>
public sealed class AdminApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded
            || !ApiV1.IsApiPath(context.Request.Path)
            || !IsAdminApiPolicy(policy))
        {
            return defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }

        return WriteProblemAsync(context, authorizeResult.Forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized);
    }

    internal static bool IsAdminApiPolicy(AuthorizationPolicy policy) =>
        policy.AuthenticationSchemes.Contains(AdminAuthenticationSchemes.CompositeScheme)
        && !policy.AuthenticationSchemes.Contains(MemberAuthenticationSchemes.MembersBearer)
        && !policy.AuthenticationSchemes.Contains(MemberAuthenticationSchemes.MembersCookie);

    internal static Task WriteProblemAsync(HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Results.Problem(
                statusCode: statusCode,
                title: ReasonPhrases.GetReasonPhrase(statusCode))
            .ExecuteAsync(context);
    }
}
