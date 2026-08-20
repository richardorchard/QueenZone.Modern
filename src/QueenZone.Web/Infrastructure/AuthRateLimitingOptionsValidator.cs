using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class AuthRateLimitingOptionsValidator : IValidateOptions<AuthRateLimitingOptions>
{
    public const int MaxPermitLimit = 10_000;

    public const int MaxWindowMinutes = 1_440;

    public ValidateOptionsResult Validate(string? name, AuthRateLimitingOptions options)
    {
        var failures = new List<string>();
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{AuthRateLimitingOptions.SectionName}:IpPermitLimit",
            options.IpPermitLimit,
            MaxPermitLimit);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{AuthRateLimitingOptions.SectionName}:IpWindowMinutes",
            options.IpWindowMinutes,
            MaxWindowMinutes);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{AuthRateLimitingOptions.SectionName}:AccountPermitLimit",
            options.AccountPermitLimit,
            MaxPermitLimit);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{AuthRateLimitingOptions.SectionName}:AccountWindowMinutes",
            options.AccountWindowMinutes,
            MaxWindowMinutes);
        return OptionsValidation.Result(failures);
    }
}
