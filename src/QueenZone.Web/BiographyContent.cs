using QueenZone.Data;

namespace QueenZone.Web;

public static class BiographyContent
{
    public static string FormatBody(string body) => NewsArticleContent.FormatBody(body);

    public static string GetDetailCanonicalPath(BiographyChapterItem chapter) =>
        BiographyRoutes.GetChapterDetailPath(chapter);

    public static string GetListSummary(BiographyChapterItem chapter) =>
        !string.IsNullOrWhiteSpace(chapter.Summary)
            ? chapter.Summary
            : LegacyArticleText.GetExcerpt(chapter.Body);
}