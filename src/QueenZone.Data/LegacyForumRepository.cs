using System.Data;
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
              AND (t.Q_FORUM_TOPIC_PARENT_ID = 0 OR t.Q_FORUM_TOPIC_PARENT_ID IS NULL)
            ORDER BY t.TOPIC_LAST_POST DESC
        ) latest
        WHERE f.Q_FORUM_ID = @Id
        """;

    public async Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(CategoriesSelect, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ForumCategoryRow>(command);
        return rows.Select(Map).ToList();
    }

    public async Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(CategoryByIdSelect, new { Id = id }, cancellationToken: cancellationToken);
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
        var parameters = new DynamicParameters();
        parameters.Add("CurrentPage", page);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Q_FORUM_ID", forumId);
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "Q_FORUM_VIEW_PAGE_SP",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<ForumTopicRow>(command);
        return new ForumCategoryTopicsPage(
            rows.Select(MapTopic).ToList(),
            parameters.Get<int>("TotalRecords"),
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

    private static ForumTopicItem MapTopic(ForumTopicRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_SUBJECT.Trim(),
            row.TOPIC_LAST_POST,
            row.USERNAME.Trim(),
            row.NUMBEROFREPLIES,
            string.IsNullOrWhiteSpace(row.LAST_POST_USERNAME) ? null : row.LAST_POST_USERNAME.Trim(),
            row.STICKY == 1);

    private sealed record ForumTopicRow(
        int Id,
        int Q_FORUM_TOPIC_ID,
        string TOPIC_SUBJECT,
        DateTime TOPIC_LAST_POST,
        int USER_ID,
        string USERNAME,
        short NUMBEROFREPLIES,
        string? LAST_POST_USERNAME,
        byte STICKY);

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