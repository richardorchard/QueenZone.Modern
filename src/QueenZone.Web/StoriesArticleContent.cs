using QueenZone.Data;

namespace QueenZone.Web;

public static class StoriesArticleContent
{
    public static string GetDetailCanonicalPath(int id, string title) =>
        $"/stories/{id}/{NewsSlug.Slugify(title)}";

    public static string FormatBody(string body) => NewsArticleContent.FormatBody(body);
}