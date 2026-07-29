using System.Text.Json;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web;

/// <summary>
/// Builds the schema.org BreadcrumbList JSON-LD that matches the visible
/// breadcrumb nav rendered by the <c>_Breadcrumbs</c> partial.
/// </summary>
public static class BreadcrumbJsonLd
{
    public static string Build(IReadOnlyList<BreadcrumbItem> items, string publicBaseUrl)
    {
        var listItems = items
            .Select((item, index) => new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Label,
                ["item"] = SiteUrl.ToAbsolute(publicBaseUrl, item.Href),
            })
            .ToList();

        var document = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = listItems,
        };

        return JsonSerializer.Serialize(document);
    }
}
