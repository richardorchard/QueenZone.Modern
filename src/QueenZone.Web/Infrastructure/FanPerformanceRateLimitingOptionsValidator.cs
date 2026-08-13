using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class FanPerformanceRateLimitingOptionsValidator
    : IValidateOptions<FanPerformanceRateLimitingOptions>
{
    public const int MaxPermitLimit = 10_000;

    public const int MaxWindowSeconds = 86_400;

    public ValidateOptionsResult Validate(string? name, FanPerformanceRateLimitingOptions options)
    {
        var failures = new List<string>();
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{FanPerformanceRateLimitingOptions.SectionName}:AudioPermitLimit",
            options.AudioPermitLimit,
            MaxPermitLimit);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{FanPerformanceRateLimitingOptions.SectionName}:AudioSlidingWindowSeconds",
            options.AudioSlidingWindowSeconds,
            MaxWindowSeconds);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{FanPerformanceRateLimitingOptions.SectionName}:BrowsePermitLimit",
            options.BrowsePermitLimit,
            MaxPermitLimit);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{FanPerformanceRateLimitingOptions.SectionName}:BrowseWindowSeconds",
            options.BrowseWindowSeconds,
            MaxWindowSeconds);
        return OptionsValidation.Result(failures);
    }
}
