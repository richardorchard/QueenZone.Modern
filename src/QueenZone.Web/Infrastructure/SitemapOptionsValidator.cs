using Microsoft.Extensions.Options;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web;

public sealed class SitemapOptionsValidator : IValidateOptions<SitemapOptions>
{
    public ValidateOptionsResult Validate(string? name, SitemapOptions options)
    {
        if (options.CacheHours <= 0)
        {
            return ValidateOptionsResult.Fail(
                $"{SitemapOptions.SectionName}:CacheHours must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
