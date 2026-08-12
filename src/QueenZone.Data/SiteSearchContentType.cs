namespace QueenZone.Data;

/// <summary>
/// Values stored in <see cref="Entities.SearchDocumentEntity.ContentType"/>. Also used as the
/// unified search page's <c>?type=</c> filter value.
/// </summary>
public static class SiteSearchContentType
{
    public const string News = "news";
    public const string Article = "article";
    public const string LegacyArticle = "legacy-article";
    public const string Forum = "forum";
    public const string Biography = "biography";
    public const string Discography = "discography";
    public const string Photo = "photo";
    public const string Timeline = "timeline";
    public const string FanPerformance = "fan-performance";

    public static readonly IReadOnlyList<string> All =
    [
        News, Article, LegacyArticle, Forum, Biography, Discography, Photo, Timeline, FanPerformance,
    ];

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        foreach (var candidate in All)
        {
            if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    public static string DisplayLabel(string contentType) => contentType switch
    {
        News => "News",
        Article => "Articles",
        LegacyArticle => "Articles",
        Forum => "Forum",
        Biography => "Biography",
        Discography => "Discography",
        Photo => "Photography",
        Timeline => "Timeline",
        FanPerformance => "Fan Performances",
        _ => contentType,
    };
}
