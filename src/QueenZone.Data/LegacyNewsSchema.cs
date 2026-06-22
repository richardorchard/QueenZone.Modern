using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public static class LegacyNewsSchema
{
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
}