using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class UploadQuotaOptionsValidator : IValidateOptions<UploadQuotaOptions>
{
    public const int MaxUploadsPerDayCeiling = 100_000;

    public const long MaxBytesPerDayCeiling = 10L * 1024 * 1024 * 1024;

    public ValidateOptionsResult Validate(string? name, UploadQuotaOptions options)
    {
        var failures = new List<string>();
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{UploadQuotaOptions.SectionName}:MaxUploadsPerDay",
            options.MaxUploadsPerDay,
            MaxUploadsPerDayCeiling);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{UploadQuotaOptions.SectionName}:MaxBytesPerDay",
            options.MaxBytesPerDay,
            MaxBytesPerDayCeiling);
        return OptionsValidation.Result(failures);
    }
}
