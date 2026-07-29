namespace QueenZone.Web;

public sealed class PublicQueryCacheOptions
{
    public const string SectionName = "PublicQueryCache";

    public TimeSpan NewsCacheDuration { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan ArticleCountCacheDuration { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan ForumStatsCacheDuration { get; init; } = TimeSpan.FromMinutes(30);

    public TimeSpan OnThisDayCacheDuration { get; init; } = TimeSpan.FromHours(12);

    /// <summary>Photography archive changes rarely; default is generous.</summary>
    public TimeSpan PhotoCacheDuration { get; init; } = TimeSpan.FromMinutes(30);
}
