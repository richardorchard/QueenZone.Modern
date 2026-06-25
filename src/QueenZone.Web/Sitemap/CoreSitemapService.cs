using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Sitemap;

public sealed class CoreSitemapService(
    CoreSitemapBuilder builder,
    IMemoryCache cache,
    IOptions<SitemapOptions> options)
{
    private const string CacheKey = "sitemap-core.xml";

    public async Task<string> GetXmlAsync(string publicBaseUrl, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKey}:{publicBaseUrl.TrimEnd('/')}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(options.Value.CacheHours);
            var sitemapEntries = await builder.BuildAsync(cancellationToken);
            return SitemapXmlWriter.WriteUrlSet(sitemapEntries, publicBaseUrl);
        }) ?? string.Empty;
    }
}