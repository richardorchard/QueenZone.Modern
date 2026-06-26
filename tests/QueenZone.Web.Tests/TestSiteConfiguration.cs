using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

internal static class TestSiteConfiguration
{
    public const string PublicBaseUrl = "https://www.queenzone.org";

    public static string CanonicalLink(string path) =>
        $"""<link rel="canonical" href="{SiteUrl.ToAbsolute(PublicBaseUrl, path)}">""";

    public static string PrevLink(string path) =>
        $"""<link rel="prev" href="{SiteUrl.ToAbsolute(PublicBaseUrl, path)}">""";

    public static string NextLink(string path) =>
        $"""<link rel="next" href="{SiteUrl.ToAbsolute(PublicBaseUrl, path)}">""";
}