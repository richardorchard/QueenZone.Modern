using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class MobileAuthOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<MobileAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, MobileAuthOptions options)
    {
        var failures = new List<string>();
        var prefix = MobileAuthOptions.SectionName;

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add($"{prefix}:ClientId is required.");
        }

        OptionsValidation.RequireNonBlankEntries(
            failures,
            $"{prefix}:RedirectUris",
            options.RedirectUris,
            requireAtLeastOne: true);

        foreach (var uri in options.RedirectUris ?? [])
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                continue;
            }

            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
                || parsed.Scheme is "javascript" or "data" or "file")
            {
                failures.Add($"{prefix}:RedirectUris entry '{uri}' must be an absolute URI with a non-script scheme.");
            }
        }

        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{prefix}:AccessTokenLifetimeMinutes",
            options.AccessTokenLifetimeMinutes,
            maximum: 120);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{prefix}:AuthorizationCodeLifetimeMinutes",
            options.AuthorizationCodeLifetimeMinutes,
            maximum: 15);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{prefix}:RefreshTokenLifetimeDays",
            options.RefreshTokenLifetimeDays,
            maximum: 90);

        if (QueenZoneEnvironments.IsProductionLike(environment)
            && !OptionsValidation.LooksConfigured(options.SigningKey))
        {
            failures.Add(
                $"{prefix}:SigningKey must be set to at least 32 characters in {environment.EnvironmentName}. " +
                "Configure MobileAuth__SigningKey via App Service application settings or Key Vault — " +
                "do not commit a production signing key.");
        }
        else if (OptionsValidation.LooksConfigured(options.SigningKey) && options.SigningKey.Trim().Length < 32)
        {
            failures.Add($"{prefix}:SigningKey must be at least 32 characters.");
        }

        return OptionsValidation.Result(failures);
    }
}
