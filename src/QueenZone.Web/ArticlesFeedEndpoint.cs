using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

public static class ArticlesFeedEndpointExtensions
{
    public static void MapArticlesFeedEndpoint(this WebApplication app)
    {
        app.MapGet("/articles/feed.rss", async (
            IArticleRepository articleRepository,
            IOptions<SiteOptions> siteOptions,
            CancellationToken cancellationToken) =>
        {
            var articles = await articleRepository.GetSitemapEntriesAsync(cancellationToken);
            var baseUrl = siteOptions.Value.PublicBaseUrl.TrimEnd('/');
            var xml = BuildRss(articles, baseUrl);
            return Results.Content(xml, "application/rss+xml; charset=utf-8");
        })
        .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);
    }

    private static string BuildRss(IReadOnlyList<PublishedArticleSubmission> articles, string baseUrl)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
        sb.AppendLine("<channel>");
        sb.AppendLine("  <title>QueenZone Community Articles</title>");
        sb.AppendLine($"  <link>{EscapeXml(baseUrl)}/articles</link>");
        sb.AppendLine("  <description>Community articles and features from QueenZone</description>");
        sb.AppendLine($"  <atom:link href=\"{EscapeXml(baseUrl)}/articles/feed.rss\" rel=\"self\" type=\"application/rss+xml\" />");

        foreach (var article in articles)
        {
            var link = $"{baseUrl}/articles/{EscapeXml(article.Slug)}";
            sb.AppendLine("  <item>");
            sb.AppendLine($"    <title>{EscapeXml(article.Title)}</title>");
            sb.AppendLine($"    <link>{link}</link>");
            sb.AppendLine($"    <guid isPermaLink=\"true\">{link}</guid>");
            sb.AppendLine($"    <pubDate>{article.PublishedAt:R}</pubDate>");
            if (!string.IsNullOrWhiteSpace(article.Excerpt))
            {
                sb.AppendLine($"    <description>{EscapeXml(article.Excerpt)}</description>");
            }
            sb.AppendLine("  </item>");
        }

        sb.AppendLine("</channel>");
        sb.AppendLine("</rss>");
        return sb.ToString();
    }

    private static string EscapeXml(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}
