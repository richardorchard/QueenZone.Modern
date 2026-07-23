namespace QueenZone.Web;

/// <summary>
/// Helpers for deciding whether Microsoft Entra (Azure AD) admin auth is configured.
/// Placeholder values from committed <c>appsettings.json</c> must not count as configured.
/// </summary>
public static class AzureAdClientId
{
    /// <summary>
    /// Returns true when <paramref name="clientId"/> is a real application client id,
    /// not empty and not a template placeholder such as <c>YOUR_CLIENT_ID</c>.
    /// </summary>
    public static bool IsConfigured(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return false;
        }

        var trimmed = clientId.Trim();
        if (trimmed.Contains("YOUR_", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "CHANGE_ME", StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, "TODO", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ensures non-Development/Testing hosts have a real Entra client id.
    /// Development may omit it and use <see cref="TestAuthHandler"/> for local admin testing.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the environment requires Entra and the client id is missing or a placeholder.
    /// </exception>
    public static void EnsureConfiguredForEnvironment(IHostEnvironment environment, string? clientId)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            return;
        }

        if (IsConfigured(clientId))
        {
            return;
        }

        throw new InvalidOperationException(
            "AzureAd:ClientId must be set to a real Microsoft Entra application (client) ID " +
            "outside Development and Testing. " +
            "Header-based TestAuthHandler (X-Test-User-Email) is only available in Development " +
            "when ClientId is empty, and in the Testing environment for automated tests. " +
            "Configure AzureAd:ClientId (and related Entra settings) in App Service or " +
            "appsettings.Local.json for staging/production hosts.");
    }
}
