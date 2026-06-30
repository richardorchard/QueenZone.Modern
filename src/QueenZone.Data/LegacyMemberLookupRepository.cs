using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Looks up a legacy USERS_T account by email so a new modern MemberAccount can be silently
/// linked to it on first sign-in. Read-only: the legacy PASSWORD column is never used, since
/// it's an unrecoverable old hash format.
/// </summary>
public sealed class LegacyMemberLookupRepository : ILegacyMemberLookupRepository
{
    private readonly string connectionString;

    public LegacyMemberLookupRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<LegacyMemberMatch?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "SELECT TOP 1 USER_ID, USERNAME FROM dbo.USERS_T WHERE EMAIL = @Email",
            new { Email = email },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(command);
        return row is null ? null : new LegacyMemberMatch(row.USER_ID, row.USERNAME?.Trim() ?? string.Empty);
    }

    private sealed class UserRow
    {
        public int USER_ID { get; set; }

        public string? USERNAME { get; set; }
    }
}
