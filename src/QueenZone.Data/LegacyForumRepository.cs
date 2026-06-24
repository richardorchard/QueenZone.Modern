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

    private const string TotalThreadCountSelect = """
        SELECT ISNULL(SUM(CAST(THREADCOUNT AS bigint)), 0)
        FROM dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V
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
        var parameters = new DynamicParameters();
        parameters.Add("CurrentPage", page);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Q_FORUM_ID", forumId);
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "Q_FORUM_VIEW_PAGE_SP",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumTopicRow>(command);
        return new ForumCategoryTopicsPage(
            rows.Select(MapTopic).ToList(),
            parameters.Get<int>("TotalRecords"),
            page,
            pageSize);
    }

    public async Task<ForumTopicPostsPage?> GetTopicPostsPageAsync(
        int topicId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("CurrentPage", page);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Q_FORUM_TOPIC_ID", topicId);
        parameters.Add("USER_ID", 0);
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("SUBSCRIBED", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("forum_name", dbType: DbType.String, size: 30, direction: ParameterDirection.Output);
        parameters.Add("SUBJECT", dbType: DbType.String, size: 75, direction: ParameterDirection.Output);
        parameters.Add("Q_FORUM_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("DISCO", dbType: DbType.Byte, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "Q_FORUM_TOPIC_NEW_SP",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumPostRow>(command);
        var forumId = parameters.Get<int?>("Q_FORUM_ID") ?? 0;
        var title = parameters.Get<string>("SUBJECT")?.Trim();
        if (forumId <= 0 || string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var header = new ForumTopicHeader(
            topicId,
            title,
            forumId,
            parameters.Get<string>("forum_name")?.Trim() ?? string.Empty);

        return new ForumTopicPostsPage(
            header,
            rows.Select(MapPost).ToList(),
            parameters.Get<int>("TotalRecords"),
            page,
            pageSize);
    }

    public async Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            TotalThreadCountSelect,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoriesAsync(cancellationToken);
        var threadCount = await GetTotalThreadCountAsync(cancellationToken);
        return ForumArchiveStats.FromCategories(categories, threadCount);
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

    private static ForumPostItem MapPost(ForumPostRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_MESSAGE?.Trim() ?? string.Empty,
            row.TOPIC_DATE,
            row.USERNAME?.Trim() ?? "Unknown",
            string.IsNullOrWhiteSpace(row.SIGNATURE) ? null : row.SIGNATURE.Trim(),
            row.NUMBER_OF_POSTS,
            row.DATE_CREATED);

    private static ForumTopicItem MapTopic(ForumTopicRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_SUBJECT?.Trim() ?? string.Empty,
            row.TOPIC_LAST_POST,
            row.USERNAME?.Trim() ?? "Unknown",
            row.NUMBEROFREPLIES,
            string.IsNullOrWhiteSpace(row.LAST_POST_USERNAME) ? null : row.LAST_POST_USERNAME.Trim(),
            row.STICKY == 1);

    private sealed record ForumPostRow(
        string? TOPIC_MESSAGE,
        DateTime TOPIC_DATE,
        int USER_ID,
        string? USERNAME,
        string? SIGNATURE,
        short NUMBER_OF_POSTS,
        DateTime? DATE_CREATED,
        int Q_FORUM_TOPIC_ID,
        string? ATTACHMENT,
        string? FILESIZE,
        short ATTACH_COUNT,
        byte ONLINE,
        string? AVATAR,
        string? DISPLAY_MESSAGE,
        byte DISCO);

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
        string? Name,
        string? Description,
        int PostCount,
        DateTime? LastActivityAt,
        string? LatestThreadTitle,
        int SortOrder);
}