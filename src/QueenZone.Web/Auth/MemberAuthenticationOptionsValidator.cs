using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class MemberAuthenticationOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<MemberAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, MemberAuthenticationOptions options)
    {
        var failures = new List<string>();
        var configuredProviders = 0;

        configuredProviders += ValidateProvider(
            failures,
            $"{MemberAuthenticationOptions.SectionName}:Google",
            options.Google);
        configuredProviders += ValidateProvider(
            failures,
            $"{MemberAuthenticationOptions.SectionName}:Microsoft",
            options.Microsoft);
        configuredProviders += ValidateProvider(
            failures,
            $"{MemberAuthenticationOptions.SectionName}:Discord",
            options.Discord);
        configuredProviders += ValidateProvider(
            failures,
            $"{MemberAuthenticationOptions.SectionName}:GitHub",
            options.GitHub);

        if (configuredProviders == 0 && QueenZoneEnvironments.IsProductionLike(environment))
        {
            failures.Add(
                $"{MemberAuthenticationOptions.SectionName} must configure at least one OAuth provider " +
                $"(ClientId and ClientSecret) in {environment.EnvironmentName}.");
        }

        return OptionsValidation.Result(failures);
    }

    private static int ValidateProvider(
        ICollection<string> failures,
        string name,
        MemberAuthenticationOptions.ProviderCredentials? provider)
    {
        var hasClientId = OptionsValidation.LooksConfigured(provider?.ClientId);
        var hasClientSecret = OptionsValidation.LooksConfigured(provider?.ClientSecret);
        if (hasClientId == hasClientSecret)
        {
            return hasClientId ? 1 : 0;
        }

        failures.Add($"{name} ClientId and ClientSecret must both be set, or both left empty.");
        return 0;
    }
}
