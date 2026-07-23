namespace QueenZone.Web.Sitemap;

public static class SitemapSections
{
    public const string News = "news";
    public const string Articles = "articles";
    public const string Biography = "biography";
    public const string ForumCategories = "forum-categories";
    public const string Photography = "photography";
    public const string FanPerformances = "fan-performances";
    public const string Discography = "discography";

    public static readonly IReadOnlyList<string> All =
    [
        News,
        Articles,
        Biography,
        ForumCategories,
        Photography,
        FanPerformances,
        Discography
    ];

    public static string GetPath(string section) => $"/sitemap-{section}.xml";
}
