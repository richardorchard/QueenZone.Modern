using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data;

/// <summary>
/// Legacy <c>NEWS_T</c> column-availability probes. Latest-row / published projection
/// SQL lives in <see cref="PublishedNewsQuery"/>.
/// </summary>
public static class LegacyNewsSchema
{
    /// <summary>
    /// Process-lifetime cache of COL_LENGTH probes keyed by connection string.
    /// Avoids a SQL round-trip on every scoped news repository construction.
    /// </summary>
    private static readonly ConcurrentDictionary<string, NewsColumnAvailability> ColumnAvailabilityCache =
        new(StringComparer.Ordinal);

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

    /// <summary>
    /// Returns whether <c>NEWS_T.SLUG</c> exists. Result is cached for the process lifetime
    /// per connection string (same probe as <see cref="GetNewsColumnAvailability"/>).
    /// </summary>
    [ExcludeFromCodeCoverage] // SQL Server COL_LENGTH probe (cached).
    internal static bool HasSlugColumn(string connectionString) =>
        GetNewsColumnAvailability(connectionString).HasSlugColumn;

    /// <summary>
    /// Probes legacy <c>NEWS_T</c> column availability once per connection string, then reuses
    /// the result for all subsequent repository constructions in this process.
    /// </summary>
    [ExcludeFromCodeCoverage] // SQL Server COL_LENGTH probe (cached).
    internal static NewsColumnAvailability GetNewsColumnAvailability(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return ColumnAvailabilityCache.GetOrAdd(connectionString, static cs => ProbeNewsColumnAvailability(cs));
    }

    /// <summary>Test helper: clears process cache so probes can be re-tested in isolation.</summary>
    internal static void ClearColumnAvailabilityCacheForTests() => ColumnAvailabilityCache.Clear();

    [ExcludeFromCodeCoverage] // SQL Server COL_LENGTH probe.
    private static NewsColumnAvailability ProbeNewsColumnAvailability(string connectionString)
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
