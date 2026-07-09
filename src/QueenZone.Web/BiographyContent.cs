using QueenZone.Data;

namespace QueenZone.Web;

public static class BiographyContent
{
    public static string FormatBody(string body) => NewsArticleContent.FormatBody(body);

    public static string GetDetailCanonicalPath(int id, string title) =>
        BiographyRoutes.GetChapterDetailPath(id, title);

    public static string GetDetailCanonicalPath(BiographyChapterItem chapter) =>
        GetDetailCanonicalPath(chapter.Id, chapter.Title);

    public static string GetDetailCanonicalPath(BiographyChapterDetail chapter) =>
        chapter.DetailPath;

    public static string GetListSummary(BiographyChapterItem chapter) =>
        !string.IsNullOrWhiteSpace(chapter.Summary)
            ? chapter.Summary
            : LegacyArticleText.GetExcerpt(chapter.Body);
}
