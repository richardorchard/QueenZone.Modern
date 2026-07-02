using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public static class LegacyNewsSchema
{
    public sealed class NewsColumnAvailability
    {
        public bool HasSourceUrlColumn { get; init; }

        public bool HasSlugColumn { get; init; }

        public bool HasCreatedAtColumn { get; init; }

        public bool HasUpdatedAtColumn { get; init; }

        public bool HasEditorEmailColumn { get; init; }
    }

    public static string BuildPublishedNewsCte(bool includeSlugColumn)
    {
        var slugProjection = includeSlugColumn
            ? "SLUG AS Slug"
            : "CAST(NULL AS nvarchar(200)) AS Slug";

        return $"""
            WITH PublishedNews AS (
                SELECT
                    NEWS_ID AS Id,
                    TITLE AS Title,
                    {slugProjection},
                    ISNULL(EXCERPT, '') AS Excerpt,
                    ISNULL(ARTICLE, '') AS Body,
                    [DATE] AS PublishedAt,
                    SOURCE_URL AS SourceUrl,
                    CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished,
                    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
                FROM NEWS_T
                WHERE DISPLAY = 1
            )
            """;
    }

    public static string BuildAdminLatestNewsSql(NewsColumnAvailability columns) =>
        BuildAdminLatestNewsCte(columns) + """

            SELECT
                NEWS_ID,
                TITLE,
                EXCERPT,
                ARTICLE,
                [DATE],
                [DATE] AS PublishedAt,
                SOURCE_URL,
                CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS DISPLAY,
                SLUG,
                CREATED_AT,
                UPDATED_AT,
                EDITOR_EMAIL,
                USER_ID,
                NEWS_ID AS NewsId,
                TYPE,
                QUEEN_ONLINE
            FROM LatestNews
            WHERE RowNumber = 1
            """;

    public static string BuildAdminLatestNewsCountSql(NewsColumnAvailability columns) =>
        BuildAdminLatestNewsCte(columns) + """

            SELECT COUNT(*)
            FROM LatestNews
            WHERE RowNumber = 1
            """;

    private static string BuildAdminLatestNewsCte(NewsColumnAvailability columns)
    {
        var sourceUrlProjection = columns.HasSourceUrlColumn
            ? "SOURCE_URL"
            : "CAST(NULL AS varchar(2000)) AS SOURCE_URL";
        var slugProjection = columns.HasSlugColumn
            ? "SLUG"
            : "CAST(NULL AS nvarchar(200)) AS SLUG";
        var createdAtProjection = columns.HasCreatedAtColumn
            ? "CREATED_AT"
            : "CAST(NULL AS datetime2) AS CREATED_AT";
        var updatedAtProjection = columns.HasUpdatedAtColumn
            ? "UPDATED_AT"
            : "CAST(NULL AS datetime2) AS UPDATED_AT";
        var editorEmailProjection = columns.HasEditorEmailColumn
            ? "EDITOR_EMAIL"
            : "CAST(NULL AS nvarchar(256)) AS EDITOR_EMAIL";

        return $"""

            WITH LatestNews AS (
                SELECT
                    NEWS_ID,
                    ISNULL(TITLE, '') AS TITLE,
                    ISNULL(EXCERPT, '') AS EXCERPT,
                    ISNULL(ARTICLE, '') AS ARTICLE,
                    [DATE],
                    {sourceUrlProjection},
                    DISPLAY,
                    {slugProjection},
                    {createdAtProjection},
                    {updatedAtProjection},
                    {editorEmailProjection},
                    USER_ID,
                    CAST(ISNULL(TYPE, 0) AS int) AS TYPE,
                    CAST(ISNULL(QUEEN_ONLINE, 0) AS int) AS QUEEN_ONLINE,
                    ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
                FROM NEWS_T
            )
            """;
    }

    internal static bool HasSlugColumn(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        const string sql = """
            SELECT CASE
                WHEN COL_LENGTH('NEWS_T', 'SLUG') IS NOT NULL THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END
            """;

        return connection.ExecuteScalar<bool>(sql);
    }

    internal static NewsColumnAvailability GetNewsColumnAvailability(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        const string sql = """
            SELECT
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'SOURCE_URL') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasSourceUrlColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'SLUG') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasSlugColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'CREATED_AT') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasCreatedAtColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'UPDATED_AT') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasUpdatedAtColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'EDITOR_EMAIL') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasEditorEmailColumn
            """;

        return connection.QuerySingle<NewsColumnAvailability>(sql);
    }
}
