using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Reads forum categories from legacy <c>Q_FORUM_T</c>.
/// Column map and stored procedures are documented in <c>docs/legacy/db-schema.txt</c>
/// and <c>docs/legacy/table-map.md</c> (equivalent to <c>Q_LIST_FORUM_SP</c> / <c>Q_FORUM_MAIN_SP</c>).
/// </summary>
public sealed class LegacyForumRepository(string connectionString) : IForumRepository
{
    private const int CommandTimeoutSeconds = 120;

    private const string CategoriesSelect = """
        SELECT
            CAST(f.Q_FORUM_ID AS int) AS Id,
            f.Q_FORUM_NAME AS Name,
            NULLIF(LTRIM(RTRIM(f.Q_FORUM_DESCRIPTION)), '') AS Description,
            ISNULL(f.Q_FORUM_POST_COUNT, 0) AS PostCount,
            f.Q_FORUM_LAST_POST AS LastActivityAt,
            CAST(NULL AS nvarchar(200)) AS LatestThreadTitle,
            ISNULL(CAST(f.FORUM_ORDER AS int), 0) AS SortOrder
        FROM Q_FORUM_T f
        ORDER BY f.FORUM_ORDER ASC, f.Q_FORUM_ID ASC
        """;

    private const string CategoryByIdSelect = """
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
              AND t.TOPIC_STARTER = 1
            ORDER BY t.TOPIC_LAST_POST DESC
        ) latest
        WHERE f.Q_FORUM_ID = @Id
        """;

    private const string CategoryTopicsSelect = """
        SELECT
            t.Q_FORUM_TOPIC_ID,
            t.TOPIC_SUBJECT,
            t.TOPIC_LAST_POST,
            u.USERNAME,
            t.TOPIC_REPLIES AS NUMBEROFREPLIES,
            lu.USERNAME AS LAST_POST_USERNAME,
            CAST(ISNULL(t.STICKY, 0) AS tinyint) AS STICKY
        FROM Q_FORUM_TOPIC_T t
        INNER JOIN USERS_T u ON t.USER_ID = u.USER_ID
        LEFT JOIN USERS_T lu ON t.LAST_USER_ID = lu.USER_ID
        WHERE t.Q_FORUM_ID = @ForumId
          AND t.TOPIC_STARTER = 1
        ORDER BY t.STICKY DESC, t.TOPIC_LAST_POST DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
        """;

    public async Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            CategoriesSelect,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        var rows = await connection.QueryAsync<ForumCategoryRow>(command);
        return rows.Select(Map).ToList();
    }

    public async Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            CategoryByIdSelect,
            new { Id = id },
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        var row = await connection.QuerySingleOrDefaultAsync<ForumCategoryRow>(command);
        return row is null ? null : Map(row);
    }

    public async Task<ForumCategoryTopicsPage> GetCategoryTopicsPageAsync(
        int forumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var offset = Math.Max(page - 1, 0) * pageSize;

        const string countSql = """
            SELECT COUNT(*)
            FROM Q_FORUM_TOPIC_T
            WHERE Q_FORUM_ID = @ForumId
              AND TOPIC_STARTER = 1
            """;

        var countCommand = new CommandDefinition(
            countSql,
            new { ForumId = forumId },
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        var totalCount = await connection.ExecuteScalarAsync<int>(countCommand);

        var topicsCommand = new CommandDefinition(
            CategoryTopicsSelect,
            new { ForumId = forumId, Offset = offset, PageSize = pageSize },
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        var rows = await connection.QueryAsync<ForumTopicRow>(topicsCommand);

        return new ForumCategoryTopicsPage(
            rows.Select(MapTopic).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                (SELECT COUNT(*) FROM Q_FORUM_T) AS ForumCount,
                (
                    SELECT COUNT(*)
                    FROM Q_FORUM_TOPIC_T
                    WHERE TOPIC_STARTER = 1
                ) AS ThreadCount,
                (SELECT ISNULL(SUM(CAST(Q_FORUM_POST_COUNT AS bigint)), 0) FROM Q_FORUM_T) AS PostCount
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        var row = await connection.QuerySingleAsync<ForumArchiveStatsRow>(command);
        return new ForumArchiveStats(row.ForumCount, row.ThreadCount, row.PostCount);
    }

    private static ForumCategoryItem Map(ForumCategoryRow row) =>
        new(
            row.Id,
            row.Name?.Trim() ?? string.Empty,
            row.Description,
            row.PostCount,
            row.LastActivityAt,
            row.LatestThreadTitle,
            row.SortOrder);

    private static ForumTopicItem MapTopic(ForumTopicRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_SUBJECT?.Trim() ?? string.Empty,
            row.TOPIC_LAST_POST,
            row.USERNAME?.Trim() ?? "Unknown",
            row.NUMBEROFREPLIES,
            string.IsNullOrWhiteSpace(row.LAST_POST_USERNAME) ? null : row.LAST_POST_USERNAME.Trim(),
            row.STICKY == 1);

    private sealed record ForumTopicRow(
        int Q_FORUM_TOPIC_ID,
        string TOPIC_SUBJECT,
        DateTime TOPIC_LAST_POST,
        string USERNAME,
        short NUMBEROFREPLIES,
        string? LAST_POST_USERNAME,
        byte STICKY);

    private sealed record ForumCategoryRow(
        int Id,
        string? Name,
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