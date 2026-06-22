using QueenZone.Data;

namespace QueenZone.Web;

public static class ArticleContent
{
    public static string GetDetailCanonicalPath(int id, string title) =>
        $"/articles/{id}/{NewsSlug.Slugify(title)}";

    public static string FormatBody(string body) => NewsArticleContent.FormatBody(body);
}