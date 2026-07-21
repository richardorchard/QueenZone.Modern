namespace QueenZone.Data;

/// <summary>
/// Single source of truth for latest-row and published-news SQL projection rules.
/// Consumed by public archive reads (<see cref="EfNewsRepository"/>), admin list
/// projections (<see cref="EfAdminNewsRepository"/>), and sitemap builders via
/// <see cref="INewsRepository"/>.
/// </summary>
/// <remarks>
/// Public reads filter to <see cref="PublishedFilter"/> then keep
/// <see cref="LatestRowFilter"/>. Admin reads omit the published filter so drafts
/// remain visible, but still use the same latest-row deduplication expression.
/// A SQL view (<c>dbo.PublishedNewsLatest</c>) is an optional follow-up if DB
/// migration is preferred over embedding the CTE in app SQL.
/// </remarks>
public static class PublishedNewsQuery
{
    /// <summary>Public visibility gate on legacy <c>NEWS_T</c>.</summary>
    public const string PublishedFilter = "DISPLAY = 1";

    /// <summary>Keep only the latest physical row per <c>NEWS_ID</c> after ROW_NUMBER.</summary>
    public const string LatestRowFilter = "RowNumber = 1";

    /// <summary>Deduplicate legacy <c>NEWS_T</c> rows that share a <c>NEWS_ID</c>.</summary>
    public const string LatestRowNumberExpression =
        "ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC)";

    /// <summary>
    /// Builds the public <c>PublishedNews</c> CTE (published rows only, latest-row numbered).
    /// </summary>
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
                    CAST(CASE WHEN {PublishedFilter} THEN 1 ELSE 0 END AS bit) AS IsPublished,
                    {LatestRowNumberExpression} AS RowNumber
                FROM NEWS_T
                WHERE {PublishedFilter}
            )
            """;
    }

    /// <summary>
    /// Builds admin latest-row SQL (all DISPLAY values, same deduplication as public).
    /// </summary>
    public static string BuildAdminLatestNewsSql(LegacyNewsSchema.NewsColumnAvailability columns) =>
        BuildAdminLatestNewsCte(columns) + $"""

            SELECT
                NEWS_ID,
                TITLE,
                EXCERPT,
                ARTICLE,
                [DATE],
                [DATE] AS PublishedAt,
                SOURCE_URL,
                CASE WHEN {PublishedFilter} THEN 1 ELSE 0 END AS DISPLAY,
                SLUG,
                CREATED_AT,
                UPDATED_AT,
                EDITOR_EMAIL,
                USER_ID,
                NEWS_ID AS NewsId,
                TYPE,
                QUEEN_ONLINE
            FROM LatestNews
            WHERE {LatestRowFilter}
            """;

    /// <summary>
    /// Builds admin latest-row count SQL using the same CTE as <see cref="BuildAdminLatestNewsSql"/>.
    /// </summary>
    public static string BuildAdminLatestNewsCountSql(LegacyNewsSchema.NewsColumnAvailability columns) =>
        BuildAdminLatestNewsCte(columns) + $"""

            SELECT COUNT(*) AS Value
            FROM LatestNews
            WHERE {LatestRowFilter}
            """;

    private static string BuildAdminLatestNewsCte(LegacyNewsSchema.NewsColumnAvailability columns)
    {
        var sourceUrlProjection = columns.HasSourceUrlColumn
            ? "SOURCE_URL"
            : $"CAST(NULL AS varchar({NewsValidation.MaxSourceUrlLength})) AS SOURCE_URL";
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
                    {LatestRowNumberExpression} AS RowNumber
                FROM NEWS_T
            )
            """;
    }
}
