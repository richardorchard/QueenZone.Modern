using System.Text.Json;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web;

/// <summary>
/// Builds schema.org JSON-LD for editorial content (NewsArticle, Article).
/// </summary>
public static class EditorialJsonLd
{
    public static string BuildNewsArticle(
        string headline,
        string canonicalPath,
        DateTime datePublished,
        string? description,
        string publicBaseUrl) =>
        Build("NewsArticle", headline, canonicalPath, datePublished, description, authorName: null, publicBaseUrl);

    public static string BuildArticle(
        string headline,
        string canonicalPath,
        DateTime datePublished,
        string? description,
        string publicBaseUrl,
        string? authorName = null) =>
        Build("Article", headline, canonicalPath, datePublished, description, authorName, publicBaseUrl);

    private static string Build(
        string schemaType,
        string headline,
        string canonicalPath,
        DateTime datePublished,
        string? description,
        string? authorName,
        string publicBaseUrl)
    {
        var document = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = schemaType,
            ["headline"] = headline,
            ["url"] = SiteUrl.ToAbsolute(publicBaseUrl, canonicalPath),
            ["datePublished"] = datePublished.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            document["description"] = description;
        }

        if (!string.IsNullOrWhiteSpace(authorName))
        {
            document["author"] = new Dictionary<string, string>
            {
                ["@type"] = "Person",
                ["name"] = authorName,
            };
        }

        document["publisher"] = new Dictionary<string, string>
        {
            ["@type"] = "Organization",
            ["name"] = "QueenZone",
            ["url"] = publicBaseUrl.TrimEnd('/'),
        };

        return JsonSerializer.Serialize(document);
    }
}
