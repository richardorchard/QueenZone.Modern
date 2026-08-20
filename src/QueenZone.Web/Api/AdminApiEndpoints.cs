using System.Security.Claims;

namespace QueenZone.Web;

/// <summary>
/// Admin-capable JSON routes under <c>/api/v1/admin</c>. v1 only exposes a status probe;
/// later admin API endpoints must be mapped on <see cref="MapAdminApiGroup"/> so they
/// inherit the same <see cref="AdminAuthenticationSchemes.Policy"/> (Entra/test admin
/// scheme + <c>Admin:AllowedEmails</c>). Member mobile JWTs are not accepted.
/// </summary>
public static class AdminApiEndpoints
{
    public const string RootPath = "/api/v1/admin";

    public static void MapAdminApiEndpoints(this WebApplication app)
    {
        var group = app.MapAdminApiGroup();
        group.MapGet("/", GetStatus)
            .WithName("GetAdminApiStatus")
            .WithSummary("Confirms admin authentication for API clients.")
            .WithDescription(
                "Requires the same Admin authorization policy as /admin Razor pages: " +
                "the Entra (or test) admin scheme plus Admin:AllowedEmails. " +
                "A member mobile access token is not sufficient, even when the token email " +
                "is on the allowlist.")
            .Produces<AdminApiStatus>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Shared group for current and future <c>/api/v1/admin</c> routes. Always applies
    /// <see cref="AdminAuthenticationSchemes.Policy"/> — never
    /// <see cref="MemberAuthenticationSchemes.MobileMemberPolicy"/>.
    /// </summary>
    public static RouteGroupBuilder MapAdminApiGroup(this WebApplication app) =>
        app.MapGroup(RootPath)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Admin")
            .RequireAuthorization(AdminAuthenticationSchemes.Policy)
            .DisableAntiforgery();

    internal static AdminApiStatus GetStatus(ClaimsPrincipal user) =>
        new(AdminAllowlist.ResolveEmail(user) ?? string.Empty);
}

public sealed record AdminApiStatus(string Email);
