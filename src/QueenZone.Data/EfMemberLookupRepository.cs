using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Looks up a legacy USERS_T account by email so a new modern MemberAccount can be silently
/// linked to it on first sign-in. Read-only: the legacy PASSWORD column is never used.
/// </summary>
public sealed class EfMemberLookupRepository : ILegacyMemberLookupRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly Func<string, FormattableString> findByEmailSql;

    public EfMemberLookupRepository(QueenZoneDbContext dbContext)
        : this(
            dbContext,
            email => $"""
                SELECT TOP 1 USER_ID, USERNAME
                FROM dbo.USERS_T
                WHERE EMAIL = {email}
                """)
    {
    }

    internal EfMemberLookupRepository(
        QueenZoneDbContext dbContext,
        Func<string, FormattableString> findByEmailSql)
    {
        this.dbContext = dbContext;
        this.findByEmailSql = findByEmailSql;
    }

    public async Task<LegacyMemberMatch?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQuery<UserRow>(findByEmailSql(email))
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : new LegacyMemberMatch(row.USER_ID, row.USERNAME?.Trim() ?? string.Empty);
    }

    internal sealed class UserRow
    {
        public int USER_ID { get; set; }

        public string? USERNAME { get; set; }
    }
}
