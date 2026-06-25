namespace QueenZone.Web.Sitemap;

public sealed class SitemapOptions
{
    public const string SectionName = "Sitemap";

    public int CacheHours { get; init; } = 24;
}