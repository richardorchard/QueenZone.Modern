using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data;

/// <summary>
/// Legacy <c>NEWS_T</c> column-availability probes. Latest-row / published projection
/// SQL lives in <see cref="PublishedNewsQuery"/>.
/// </summary>
public static class LegacyNewsSchema
{
    public sealed class NewsColumnAvailability
    {
        public bool HasSourceUrlColumn { get; set; }

        public bool HasSlugColumn { get; set; }

        public bool HasCreatedAtColumn { get; set; }

        public bool HasUpdatedAtColumn { get; set; }

        public bool HasEditorEmailColumn { get; set; }
    }

    /// <inheritdoc cref="PublishedNewsQuery.BuildPublishedNewsCte"/>
    public static string BuildPublishedNewsCte(bool includeSlugColumn) =>
        PublishedNewsQuery.BuildPublishedNewsCte(includeSlugColumn);

    /// <inheritdoc cref="PublishedNewsQuery.BuildAdminLatestNewsSql"/>
    public static string BuildAdminLatestNewsSql(NewsColumnAvailability columns) =>
        PublishedNewsQuery.BuildAdminLatestNewsSql(columns);

    /// <inheritdoc cref="PublishedNewsQuery.BuildAdminLatestNewsCountSql"/>
    public static string BuildAdminLatestNewsCountSql(NewsColumnAvailability columns) =>
        PublishedNewsQuery.BuildAdminLatestNewsCountSql(columns);

    [ExcludeFromCodeCoverage] // SQL Server COL_LENGTH probe.
    internal static bool HasSlugColumn(string connectionString)
    {
        const string sql = """
            SELECT CASE
                WHEN COL_LENGTH('NEWS_T', 'SLUG') IS NOT NULL THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END
            """;

        return EfSql.ExecuteScalarBoolSqlAsync(connectionString, sql).GetAwaiter().GetResult();
    }

    [ExcludeFromCodeCoverage] // SQL Server COL_LENGTH probe.
    internal static NewsColumnAvailability GetNewsColumnAvailability(string connectionString)
    {
        const string sql = """
            SELECT
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'SOURCE_URL') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasSourceUrlColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'SLUG') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasSlugColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'CREATED_AT') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasCreatedAtColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'UPDATED_AT') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasUpdatedAtColumn,
                CAST(CASE WHEN COL_LENGTH('NEWS_T', 'EDITOR_EMAIL') IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasEditorEmailColumn
            """;

        return EfSql.QuerySingleSqlAsync<NewsColumnAvailability>(connectionString, sql).GetAwaiter().GetResult();
    }
}
