using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QueenZone.Storage;

namespace QueenZone.Web;

public sealed class BlobUploadOptionsValidator(IHostEnvironment environment, IConfiguration configuration)
    : IValidateOptions<BlobUploadOptions>
{
    public const long MaxDefaultBytesCeiling = 100L * 1024 * 1024;

    public const long MaxEditorBytesCeiling = 50L * 1024 * 1024;

    public const long MaxContainerBytesCeiling = 100L * 1024 * 1024;

    public ValidateOptionsResult Validate(string? name, BlobUploadOptions options)
    {
        var failures = new List<string>();
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{BlobUploadOptions.SectionName}:DefaultMaxBytes",
            options.DefaultMaxBytes,
            MaxDefaultBytesCeiling);
        OptionsValidation.RequirePositiveAtMost(
            failures,
            $"{BlobUploadOptions.SectionName}:EditorMaxBytes",
            options.EditorMaxBytes,
            MaxEditorBytesCeiling);

        OptionsValidation.RequireNonBlankEntries(
            failures,
            $"{BlobUploadOptions.SectionName}:DefaultAllowedContentTypes",
            options.DefaultAllowedContentTypes,
            requireAtLeastOne: true);

        foreach (var (containerName, policy) in options.Containers)
        {
            if (string.IsNullOrWhiteSpace(containerName))
            {
                failures.Add($"{BlobUploadOptions.SectionName}:Containers keys must not be blank.");
                continue;
            }

            if (policy.MaxBytes is { } maxBytes)
            {
                OptionsValidation.RequirePositiveAtMost(
                    failures,
                    $"{BlobUploadOptions.SectionName}:Containers:{containerName}:MaxBytes",
                    maxBytes,
                    MaxContainerBytesCeiling);
            }

            if (policy.AllowedContentTypes is not null)
            {
                OptionsValidation.RequireNonBlankEntries(
                    failures,
                    $"{BlobUploadOptions.SectionName}:Containers:{containerName}:AllowedContentTypes",
                    policy.AllowedContentTypes,
                    requireAtLeastOne: true);
            }
        }

        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            && (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            failures.Add($"{BlobUploadOptions.SectionName}:PublicBaseUrl must be an absolute http(s) URL when set.");
        }

        if (QueenZoneEnvironments.IsProductionLike(environment)
            && !QueenZoneEnvironments.UsesInMemoryBlobStorage(environment)
            && !OptionsValidation.LooksConfigured(
                configuration.GetConnectionString(BlobUploadOptions.ConnectionStringName)))
        {
            failures.Add(
                $"ConnectionStrings:{BlobUploadOptions.ConnectionStringName} is required in {environment.EnvironmentName} " +
                "when UGC uploads are enabled. Set ConnectionStrings__BlobStorage via App Service application settings " +
                "or Key Vault references. Development, Testing, and E2E may boot with an empty connection string.");
        }

        return OptionsValidation.Result(failures);
    }
}
