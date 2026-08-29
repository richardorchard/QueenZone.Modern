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

    public const string LatestArticlesSegment = Prefix + ":articles:latest";

    public const string ForumCategories = Prefix + ":forum:categories";

    public const string ForumThreadCount = Prefix + ":forum:thread-count";

    public const string ForumRecentThreadsSegment = Prefix + ":forum:recent-threads";

    public const string PhotoVersion = Prefix + ":photo:version";

    public const string HistoryVersion = Prefix + ":history:version";

    public const string PhotoCategoriesSegment = Prefix + ":photo:categories";

    public const string PhotoCategoryPageSegment = Prefix + ":photo:category-page";

    public const string LiveActivityNewForumReplies = Prefix + ":live-activity:new-forum-replies";

    public static string LatestNews(string version, int count) =>
        $"{LatestNewsSegment}:v{version}:{count}";

    public static string NewsPublishedCount(string version) =>
        $"{NewsPublishedCountSegment}:v{version}";

    public static string LatestArticles(int count) =>
        $"{LatestArticlesSegment}:{count}";

    public static string PhotoCategories(string version) =>
        $"{PhotoCategoriesSegment}:v{version}";

    public static string PhotoCategoryPage(
        string version,
        int catId,
        int page,
        int pageSize,
        string? sizeFilter = null) =>
        $"{PhotoCategoryPageSegment}:v{version}:{catId}:{page}:{pageSize}:size={sizeFilter ?? string.Empty}";

    public static string OnThisDay(string version, DateOnly date, int count) =>
        $"{Prefix}:history:on-this-day:v{version}:{date:yyyyMMdd}:{count}";

    public static string AroundThisDay(string version, DateOnly date, int dayWindow, int count) =>
        $"{Prefix}:history:around-this-day:v{version}:{date:yyyyMMdd}:{dayWindow}:{count}";

    public static string ForumRecentThreads(int count) =>
        $"{ForumRecentThreadsSegment}:{count}";
}
