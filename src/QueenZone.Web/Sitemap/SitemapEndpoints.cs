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
        });

        app.MapGet("/sitemap.xml", async (
            SitemapIndexBuilder indexBuilder,
            IOptions<SiteOptions> options,
            CancellationToken cancellationToken) =>
        {
            var entries = await indexBuilder.BuildAsync(cancellationToken);
            var xml = SitemapXmlWriter.WriteSitemapIndex(entries, options.Value.PublicBaseUrl);
            return Results.Content(xml, "application/xml; charset=utf-8");
        });

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
        });

        app.MapGet("/sitemap-core.xml", async (
            CoreSitemapBuilder builder,
            IOptions<SiteOptions> options,
            CancellationToken cancellationToken) =>
        {
            var entries = await builder.BuildAsync(cancellationToken);
            var xml = SitemapXmlWriter.WriteUrlSet(entries, options.Value.PublicBaseUrl);
            return Results.Content(xml, "application/xml; charset=utf-8");
        });
    }
}