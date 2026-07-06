using Microsoft.Extensions.Options;

namespace QueenZone.Web.Sitemap;

public static class SitemapEndpoints
{
    public static void MapSitemapEndpoints(this WebApplication app)
    {
        app.MapGet("/robots.txt", (IOptions<SiteOptions> options) =>
        {
            var baseUrl = options.Value.PublicBaseUrl.TrimEnd('/');
            var body = $"""
                User-agent: *
                Allow: /

                Disallow: /admin/
                Disallow: /health

                Sitemap: {baseUrl}/sitemap.xml
                """;

            return Results.Text(body, "text/plain");
        })
        .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);

        app.MapGet("/sitemap.xml", async (
            SitemapIndexBuilder indexBuilder,
            IOptions<SiteOptions> options,
            CancellationToken cancellationToken) =>
        {
            var entries = await indexBuilder.BuildAsync(cancellationToken);
            var xml = SitemapXmlWriter.WriteSitemapIndex(entries, options.Value.PublicBaseUrl);
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);

        app.MapGet("/sitemap-forum-{fileNumber:int}.xml", async (
            int fileNumber,
            ForumSitemapBuilder builder,
            IOptions<SiteOptions> options,
            CancellationToken cancellationToken) =>
        {
            var entries = await builder.BuildFileAsync(fileNumber, cancellationToken);
            if (entries is null || entries.Count == 0)
            {
                return Results.NotFound();
            }

            var xml = SitemapXmlWriter.WriteUrlSet(entries, options.Value.PublicBaseUrl);
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);

        app.MapGet("/sitemap-core.xml", async (
            HttpContext httpContext,
            CoreSitemapService sitemapService,
            IOptions<SiteOptions> options,
            IOptions<SitemapOptions> sitemapOptions,
            CancellationToken cancellationToken) =>
        {
            var maxAgeSeconds = Math.Max(sitemapOptions.Value.CacheHours, 1) * 3600;
            httpContext.Response.Headers.CacheControl = $"public, max-age={maxAgeSeconds}";

            var xml = await sitemapService.GetXmlAsync(options.Value.PublicBaseUrl, cancellationToken);
            return Results.Content(xml, "application/xml; charset=utf-8");
        })
        .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);
    }
}
