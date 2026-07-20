using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfMemberAccountRepository(QueenZoneDbContext dbContext) : IMemberAccountRepository
{
    public async Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.NormalizedEmail == Normalize(email), cancellationToken);

    public async Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        var login = await dbContext.MemberExternalLogins
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey, cancellationToken);

        return login is null ? null : await FindByIdAsync(login.MemberAccountId, cancellationToken);
    }

    public async Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default)
    {
        account.NormalizedEmail = Normalize(account.Email);
        dbContext.MemberAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default)
    {
        dbContext.MemberExternalLogins.Add(new MemberExternalLogin
        {
            Id = Guid.NewGuid(),
            MemberAccountId = memberAccountId,
            Provider = provider,
            ProviderKey = providerKey,
            Email = email,
            LinkedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default) =>
        await dbContext.MemberExternalLogins
            .AsNoTracking()
            .Where(login => login.MemberAccountId == memberAccountId)
            .Select(login => login.Provider)
            .Distinct()
            .OrderBy(provider => provider)
            .ToListAsync(cancellationToken);

    public async Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default)
    {
        // Load tracked so change detection persists the new name.
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.DisplayName = displayName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.AvatarUrl = avatarBlobPath;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is not null)
        {
            account.LastLoginAt = loginAt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var today = utcNow.Date;
        var sevenDaysAgo = today.AddDays(-7);
        var thirtyDaysAgo = today.AddDays(-30);

        var total = await dbContext.MemberAccounts.CountAsync(cancellationToken);
        var newToday = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= today, cancellationToken);
        var newLast7 = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= sevenDaysAgo, cancellationToken);
        var newLast30 = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= thirtyDaysAgo, cancellationToken);

        return new MemberStats(total, newToday, newLast7, newLast30);
    }

    public async Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(a => a.LastLoginAt != null)
            .OrderByDescending(a => a.LastLoginAt)
            .Take(count)
            .Select(a => new RecentLogin(a.Id, a.DisplayName, a.AvatarUrl != null, a.LastLoginAt!.Value))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rawDates = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(a => a.CreatedAt >= from)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return rawDates
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyRegistration(g.Key, g.Count()))
            .OrderBy(r => r.Date)
            .ToList();
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
