using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Reads the public forum archive from the imported <c>ModernForum*</c> tables.
/// Stored procedures and supporting indexes are documented in <c>docs/sql/006-modern-forum-read-path.sql</c>.
/// </summary>
public sealed class ModernForumRepository(string connectionString) : IForumRepository
{
    private const int CommandTimeoutSeconds = 120;

    public async Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "ModernForum_GetCategories",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumCategoryRow>(command);
        return rows.Select(Map).ToList();
    }

    public async Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("Q_FORUM_ID", id);

        var command = new CommandDefinition(
            "ModernForum_GetCategoryByLegacyForumId",
            parameters,
            commandType: CommandType.StoredProcedure,
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
            "ModernForum_GetCategoryThreadsPage",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumTopicRow>(command);
        return new ForumCategoryTopicsPage(
            rows.Select(MapTopic).ToList(),
            parameters.Get<int?>("TotalRecords") ?? 0,
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
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("forum_name", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
        parameters.Add("SUBJECT", dbType: DbType.String, size: 200, direction: ParameterDirection.Output);
        parameters.Add("Q_FORUM_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("DISCO", dbType: DbType.Byte, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "ModernForum_GetTopicPostsPage",
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
            parameters.Get<int?>("TotalRecords") ?? 0,
            page,
            pageSize);
    }

    public async Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "ModernForum_GetTotalThreadCount",
            commandType: CommandType.StoredProcedure,
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

    public async Task<int> GetTopicSitemapCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "ModernForum_GetTopicSitemapCount",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<ForumTopicSitemapItem>> GetTopicSitemapPageAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("Offset", Math.Max(offset, 0));
        parameters.Add("PageSize", pageSize);

        var command = new CommandDefinition(
            "ModernForum_GetTopicSitemapPage",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumTopicSitemapRow>(command);
        return rows
            .Select(row => new ForumTopicSitemapItem(
                row.TopicId,
                row.Title?.Trim() ?? string.Empty,
                row.LastActivityAt))
            .ToList();
    }

    public async Task<ForumSearchPage> SearchForumAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new ForumSearchPage([], 0, page, pageSize);
        }

        return await ExecuteSearchAsync(query, page, pageSize, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private async Task<ForumSearchPage> ExecuteSearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var offset = Math.Max(page - 1, 0) * pageSize;
        var parameters = new DynamicParameters();
        parameters.Add("Query", query);
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "ModernForum_SearchThreads",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken,
            commandTimeout: CommandTimeoutSeconds);

        var rows = await connection.QueryAsync<ForumSearchRow>(command);
        var totalRecords = parameters.Get<int?>("TotalRecords") ?? 0;

        return new ForumSearchPage(
            rows.Select(MapSearch).ToList(),
            totalRecords,
            page,
            pageSize);
    }

    private static ForumCategoryItem Map(ForumCategoryRow row) =>
        new(
            row.Id,
            row.Name?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(row.Description) ? null : row.Description.Trim(),
            row.PostCount,
            row.LastActivityAt,
            string.IsNullOrWhiteSpace(row.LatestThreadTitle) ? null : row.LatestThreadTitle.Trim(),
            row.SortOrder);

    private static ForumPostItem MapPost(ForumPostRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_MESSAGE?.Trim() ?? string.Empty,
            row.TOPIC_DATE ?? DateTime.MinValue,
            row.USERNAME?.Trim() ?? "Unknown",
            string.IsNullOrWhiteSpace(row.SIGNATURE) ? null : row.SIGNATURE.Trim(),
            row.NUMBER_OF_POSTS ?? 0,
            row.DATE_CREATED,
            ParseAttachments(row.ATTACHMENT, row.FILESIZE));

    private static IReadOnlyList<ForumPostAttachment>? ParseAttachments(string? attachment, string? filesize)
    {
        if (string.IsNullOrWhiteSpace(attachment))
        {
            return null;
        }

        long? bytes = long.TryParse(filesize?.Trim(), out var parsed) ? parsed : null;
        return [new ForumPostAttachment(attachment.Trim(), bytes)];
    }

    private static ForumTopicItem MapTopic(ForumTopicRow row) =>
        new(
            row.Q_FORUM_TOPIC_ID,
            row.TOPIC_SUBJECT?.Trim() ?? string.Empty,
            row.TOPIC_LAST_POST ?? DateTime.MinValue,
            row.USERNAME?.Trim() ?? "Unknown",
            row.NUMBEROFREPLIES,
            string.IsNullOrWhiteSpace(row.LAST_POST_USERNAME) ? null : row.LAST_POST_USERNAME.Trim(),
            row.STICKY == 1);

    private sealed record ForumPostRow(
        string? TOPIC_MESSAGE,
        DateTime? TOPIC_DATE,
        int? USER_ID,
        string? USERNAME,
        string? SIGNATURE,
        int? NUMBER_OF_POSTS,
        DateTime? DATE_CREATED,
        int Q_FORUM_TOPIC_ID,
        string? ATTACHMENT,
        string? FILESIZE,
        int ATTACH_COUNT,
        byte ONLINE,
        string? AVATAR,
        string? DISPLAY_MESSAGE,
        byte DISCO);

    private sealed record ForumTopicRow(
        int Id,
        int Q_FORUM_TOPIC_ID,
        string? TOPIC_SUBJECT,
        DateTime? TOPIC_LAST_POST,
        int? USER_ID,
        string? USERNAME,
        int NUMBEROFREPLIES,
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

    [ExcludeFromCodeCoverage]
    private static ForumSearchResult MapSearch(ForumSearchRow row) =>
        new(
            row.TopicId,
            row.Title?.Trim() ?? string.Empty,
            row.CategoryId,
            row.CategoryName?.Trim() ?? string.Empty,
            row.ReplyCount,
            row.LastActivityAt,
            string.IsNullOrWhiteSpace(row.StartedByDisplayName) ? null : row.StartedByDisplayName.Trim());

    [ExcludeFromCodeCoverage]
    private sealed record ForumSearchRow(
        int TopicId,
        string? Title,
        int CategoryId,
        string? CategoryName,
        int ReplyCount,
        DateTime? LastActivityAt,
        string? StartedByDisplayName);

    private sealed record ForumTopicSitemapRow(
        int TopicId,
        string? Title,
        DateTime? LastActivityAt);
}
