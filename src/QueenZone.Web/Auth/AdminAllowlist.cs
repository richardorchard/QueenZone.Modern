using System.Security.Claims;

namespace QueenZone.Web;

/// <summary>
/// Shared <c>Admin:AllowedEmails</c> check for Razor admin pages, JSON admin API routes,
/// and any other surface that must not treat "signed in" as "is an admin".
/// </summary>
public static class AdminAllowlist
{
    public static bool IsAllowed(ClaimsPrincipal? user, AdminOptions? options) =>
        IsAllowed(user, options?.AllowedEmails);

    public static bool IsAllowed(ClaimsPrincipal? user, IEnumerable<string>? allowedEmails)
    {
        var email = ResolveEmail(user);
        if (string.IsNullOrWhiteSpace(email) || allowedEmails is null)
        {
            return false;
        }

        return allowedEmails.Any(allowed =>
            !string.IsNullOrWhiteSpace(allowed)
            && string.Equals(allowed, email, StringComparison.OrdinalIgnoreCase));
    }

    public static string? ResolveEmail(ClaimsPrincipal? user) =>
        user?.FindFirstValue(ClaimTypes.Email)
        ?? user?.FindFirstValue("preferred_username")
        ?? user?.Identity?.Name;
}
