using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data;

/// <summary>
/// Production SQL Server query text for EF public-read repositories.
/// Excluded from coverage: exercised against real SQL Server in opt-in probes / production,
/// while deterministic tests inject SQLite-compatible SQL via repository test constructors.
/// Dynamic ints use EF <c>{0}</c> placeholders (passed to <c>SqlQueryRaw</c>), not string interpolation.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EfProductionSql
{
    public static (
        string Latest,
        string Count,
        string ArchivePage,
        string ById,
        string Sitemap)
        CreateArticlesQueries()
    {
        const string publishedSelect = """
            SELECT
                CAST(a.Q_ARTICLE_ID AS int) AS Id,
                a.ARTICLE_NAME AS Title,
                ISNULL(a.ARTICLE_TEXT, '') AS Body,
                a.DATE_CREATED AS PublishedAt,
                NULLIF(LTRIM(RTRIM(a.SOURCE)), '') AS Source,
                NULLIF(LTRIM(RTRIM(c.ARTICLE_CATEGORY)), '') AS CategoryName,
                CAST(CASE WHEN a.DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished
            FROM Q_ARTICLE_T a
            LEFT JOIN Q_ARTICLE_CATEGORY_T c
                ON c.Q_ARTICLE_CAT_ID = a.Q_ARTICLE_CATEGORY_ID
            WHERE a.DISPLAY = 1
            """;

        return (
            publishedSelect + """

                ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
                OFFSET 0 ROWS FETCH NEXT {0} ROWS ONLY
                """,
            """
            SELECT COUNT(*) AS Value
            FROM Q_ARTICLE_T
            WHERE DISPLAY = 1
            """,
            publishedSelect + """

                ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
                OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
                """,
            publishedSelect + """

                  AND a.Q_ARTICLE_ID = {0}
                """,
            """
            SELECT
                CAST(a.Q_ARTICLE_ID AS int) AS Id,
                a.ARTICLE_NAME AS Title,
                a.DATE_CREATED AS PublishedAt,
                CAST(NULL AS nvarchar(200)) AS Slug
            FROM Q_ARTICLE_T a
            WHERE a.DISPLAY = 1
            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            """);
    }

    public static (
        string Latest,
        string Count,
        string ArchivePage,
        string ById,
        string Sitemap)
        CreateNewsQueries(string publishedNewsCte) =>
        (
            // $$ raw strings: {{expr}} interpolates; single {0} stays a literal EF parameter placeholder.
            publishedNewsCte + $$"""

                SELECT TOP ({0})
                    Id,
                    Title,
                    Excerpt,
                    Body,
                    PublishedAt,
                    SourceUrl,
                    IsPublished,
                    Slug
                FROM PublishedNews
                WHERE {{PublishedNewsQuery.LatestRowFilter}}
                ORDER BY PublishedAt DESC, Id DESC
                """,
            publishedNewsCte + $$"""

                SELECT COUNT(*) AS Value
                FROM PublishedNews
                WHERE {{PublishedNewsQuery.LatestRowFilter}}
                """,
            publishedNewsCte + $$"""

                SELECT
                    Id,
                    Title,
                    Excerpt,
                    Body,
                    PublishedAt,
                    SourceUrl,
                    IsPublished,
                    Slug
                FROM PublishedNews
                WHERE {{PublishedNewsQuery.LatestRowFilter}}
                ORDER BY PublishedAt DESC, Id DESC
                OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
                """,
            publishedNewsCte + $$"""

                SELECT
                    Id,
                    Title,
                    Excerpt,
                    Body,
                    PublishedAt,
                    SourceUrl,
                    IsPublished,
                    Slug
                FROM PublishedNews
                WHERE {{PublishedNewsQuery.LatestRowFilter}}
                  AND Id = {0}
                """,
            publishedNewsCte + $$"""

                SELECT
                    Id,
                    Title,
                    PublishedAt,
                    Slug
                FROM PublishedNews
                WHERE {{PublishedNewsQuery.LatestRowFilter}}
                ORDER BY PublishedAt DESC, Id DESC
                """);

    public static (string ListSql, Func<int, FormattableString> DisplaySql, Func<int, FormattableString> SongsSql)
        CreateDiscographyQueries() =>
        (
            "EXEC Q_ALBUM_LIST_SP",
            static id => $"EXEC Q_ALBUM_T_DISPLAY_SP @Q_ALBUM_ID = {id}",
            static id => $"EXEC Q_ALBUM_SONG_T_LIST_SP @Q_ALBUM_ID = {id}");

    public static (bool UseProcs, string PageSelect, string CountSql, Func<int, FormattableString> ByIdSql)
        CreateFanPerformanceQueries() =>
        (
            true,
            string.Empty,
            "SELECT COUNT(*) AS Value FROM dbo.Q_STAGE_T WHERE DISPLAY = 1",
            static id => $"""
                SELECT Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED
                FROM dbo.Q_STAGE_T
                WHERE Q_STAGE_ID = {id} AND DISPLAY = 1
                """);

    public static PhotoSqlQueries CreatePhotoQueries() => PhotoSqlQueries.CreateProduction();

    public static Func<string, FormattableString> CreateMemberLookupSql() =>
        email => $"""
            SELECT TOP 1 USER_ID, USERNAME
            FROM dbo.USERS_T
            WHERE EMAIL = {email}
            """;

    public static (string ListSql, Func<short, FormattableString> DetailSql) CreateBiographyQueries() =>
        (
            "EXEC Q_BIO_LIST_SP",
            static id => $"EXEC Q_BIO_DISPLAY_SP @Q_BIO_ID = {id}");
}
