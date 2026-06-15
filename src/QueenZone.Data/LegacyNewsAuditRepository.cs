using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyNewsAuditRepository(string connectionString) : INewsAuditRepository
{
    public async Task AppendAsync(
        int newsId,
        string action,
        string actorEmail,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO NewsAuditLog (NewsId, Action, ActorEmail, Details)
            VALUES (@NewsId, @Action, @ActorEmail, @Details)
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { NewsId = newsId, Action = action, ActorEmail = actorEmail, Details = details },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyList<NewsAuditEntry>> GetByNewsIdAsync(int newsId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                Id,
                NewsId,
                Action,
                ActorEmail,
                OccurredAt,
                Details
            FROM NewsAuditLog
            WHERE NewsId = @NewsId
            ORDER BY OccurredAt DESC, Id DESC
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { NewsId = newsId }, cancellationToken: cancellationToken);
        var entries = await connection.QueryAsync<NewsAuditEntry>(command);
        return entries.AsList();
    }
}