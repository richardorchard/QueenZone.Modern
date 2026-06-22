using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyForumRepository(string connectionString) : IForumRepository
{
    private const string CategoriesSelect = """
        SELECT
            CAST(f.Q_FORUM_ID AS int) AS Id,
            f.Q_FORUM_NAME AS Name,
            NULLIF(LTRIM(RTRIM(f.Q_FORUM_DESCRIPTION)), '') AS Description,
            ISNULL(f.Q_FORUM_POST_COUNT, 0) AS PostCount,
            f.Q_FORUM_LAST_POST AS LastActivityAt,
            NULLIF(LTRIM(RTRIM(latest.TOPIC_SUBJECT)), '') AS LatestThreadTitle,
            ISNULL(CAST(f.FORUM_ORDER AS int), 0) AS SortOrder
        FROM Q_FORUM_T f
        OUTER APPLY (
            SELECT TOP 1 t.TOPIC_SUBJECT
            FROM Q_FORUM_TOPIC_T t
            WHERE t.Q_FORUM_ID = f.Q_FORUM_ID
              AND (t.Q_FORUM_TOPIC_PARENT_ID = 0 OR t.Q_FORUM_TOPIC_PARENT_ID IS NULL)
            ORDER BY t.TOPIC_LAST_POST DESC
        ) latest
        ORDER BY f.FORUM_ORDER ASC, f.Q_FORUM_ID ASC
        """;

    public async Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(CategoriesSelect, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ForumCategoryRow>(command);
        return rows.Select(Map).ToList();
    }

    public async Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM Q_FORUM_T) AS ForumCount,
                (
                    SELECT COUNT(*)
                    FROM Q_FORUM_TOPIC_T
                    WHERE Q_FORUM_TOPIC_PARENT_ID = 0 OR Q_FORUM_TOPIC_PARENT_ID IS NULL
                ) AS ThreadCount,
                (SELECT ISNULL(SUM(CAST(Q_FORUM_POST_COUNT AS bigint)), 0) FROM Q_FORUM_T) AS PostCount
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleAsync<ForumArchiveStatsRow>(command);
        return new ForumArchiveStats(row.ForumCount, row.ThreadCount, row.PostCount);
    }

    private static ForumCategoryItem Map(ForumCategoryRow row) =>
        new(
            row.Id,
            row.Name.Trim(),
            row.Description,
            row.PostCount,
            row.LastActivityAt,
            row.LatestThreadTitle,
            row.SortOrder);

    private sealed record ForumCategoryRow(
        int Id,
        string Name,
        string? Description,
        int PostCount,
        DateTime? LastActivityAt,
        string? LatestThreadTitle,
        int SortOrder);

    private sealed record ForumArchiveStatsRow(
        int ForumCount,
        int ThreadCount,
        long PostCount);
}