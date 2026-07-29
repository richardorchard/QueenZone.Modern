namespace QueenZone.Web;

/// <summary>
/// Centralized key conventions for <see cref="PublicQueryCacheService"/>.
/// News entries are versioned so invalidation does not depend on hard-coded caller counts.
/// </summary>
public static class PublicQueryCacheKeys
{
    public const string Prefix = "public-query";

    public const string NewsVersion = Prefix + ":news:version";

    public const string LatestNewsSegment = Prefix + ":news:latest";

    public const string NewsPublishedCountSegment = Prefix + ":news:published-count";

    public const string ArticlePublishedCount = Prefix + ":articles:published-count";

    public const string ForumCategories = Prefix + ":forum:categories";

    public const string ForumThreadCount = Prefix + ":forum:thread-count";

    public const string PhotoVersion = Prefix + ":photo:version";

    public const string PhotoCategoriesSegment = Prefix + ":photo:categories";

    public const string PhotoCategoryPageSegment = Prefix + ":photo:category-page";

    public static string LatestNews(string version, int count) =>
        $"{LatestNewsSegment}:v{version}:{count}";

    public static string NewsPublishedCount(string version) =>
        $"{NewsPublishedCountSegment}:v{version}";

    public static string PhotoCategories(string version) =>
        $"{PhotoCategoriesSegment}:v{version}";

    public static string PhotoCategoryPage(string version, int catId, int page, int pageSize) =>
        $"{PhotoCategoryPageSegment}:v{version}:{catId}:{page}:{pageSize}";

    public static string OnThisDay(DateOnly date, int count) =>
        $"{Prefix}:history:on-this-day:{date:yyyyMMdd}:{count}";

    public static string AroundThisDay(DateOnly date, int dayWindow, int count) =>
        $"{Prefix}:history:around-this-day:{date:yyyyMMdd}:{dayWindow}:{count}";
}
