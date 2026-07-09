using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class SiteOptionsValidator : IValidateOptions<SiteOptions>
{
    public ValidateOptionsResult Validate(string? name, SiteOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return ValidateOptionsResult.Fail(
                $"{SiteOptions.SectionName}:PublicBaseUrl is required.");
        }

        if (!Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ValidateOptionsResult.Fail(
                $"{SiteOptions.SectionName}:PublicBaseUrl must be an absolute http(s) URL.");
        }

        return ValidateOptionsResult.Success;
    }
}
