using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

/// <summary>
/// Validates admin allowlist. Production-like environments require a non-empty list
/// (from App Service / Key Vault), while Development may ship empty until Local.json is set.
/// </summary>
public sealed class AdminOptionsValidator(IHostEnvironment environment) : IValidateOptions<AdminOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminOptions options)
    {
        var emails = options.AllowedEmails ?? [];

        if (emails.Any(string.IsNullOrWhiteSpace))
        {
            return ValidateOptionsResult.Fail(
                $"{AdminOptions.SectionName}:AllowedEmails must not contain blank entries.");
        }

        // Committed appsettings.json ships an empty allowlist on purpose. Production must
        // supply Admin__AllowedEmails__N via App Service application settings or Key Vault.
        if (emails.Length == 0 && IsProductionLike(environment))
        {
            return ValidateOptionsResult.Fail(
                $"{AdminOptions.SectionName}:AllowedEmails must contain at least one admin email in {environment.EnvironmentName}. " +
                "Configure Admin__AllowedEmails__0 (and further indexes) via App Service application settings or Key Vault references — " +
                "do not rely on committed appsettings alone.");
        }

        return ValidateOptionsResult.Success;
    }

    internal static bool IsProductionLike(IHostEnvironment environment) =>
        environment.IsProduction()
        || environment.IsEnvironment("Staging")
        || string.Equals(environment.EnvironmentName, "Preview", StringComparison.OrdinalIgnoreCase);
}
