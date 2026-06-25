using QueenZone.Data;

namespace QueenZone.Web;

public static class BiographyContent
{
    public static string FormatBody(string body) => NewsArticleContent.FormatBody(body);

    public static string GetDetailCanonicalPath(BiographyChapterItem chapter) =>
        BiographyRoutes.GetChapterDetailPath(chapter);
}