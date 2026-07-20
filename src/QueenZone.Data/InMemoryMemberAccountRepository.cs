using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryMemberAccountRepository : IMemberAccountRepository
{
    private readonly List<MemberAccount> accounts = [];
    private readonly List<MemberExternalLogin> externalLogins = [];
    private readonly Lock gate = new();

    public Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.NormalizedEmail == Normalize(email));
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == id);
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var login = externalLogins.FirstOrDefault(l => l.Provider == provider && l.ProviderKey == providerKey);
            var account = login is null ? null : accounts.FirstOrDefault(a => a.Id == login.MemberAccountId);
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            account.NormalizedEmail = Normalize(account.Email);
            accounts.Add(account);
            return Task.FromResult(account);
        }
    }

    public Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            externalLogins.Add(new MemberExternalLogin
            {
                Id = Guid.NewGuid(),
                MemberAccountId = memberAccountId,
                Provider = provider,
                ProviderKey = providerKey,
                Email = email,
                LinkedAt = DateTime.UtcNow,
            });
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<string> providers = externalLogins
                .Where(login => login.MemberAccountId == memberAccountId)
                .Select(login => login.Provider)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(provider => provider, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(providers);
        }
    }

    public Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.DisplayName = displayName;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.AvatarUrl = avatarBlobPath;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is not null)
            {
                account.LastLoginAt = loginAt;
            }

            return Task.CompletedTask;
        }
    }

    public Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var today = utcNow.Date;
            var stats = new MemberStats(
                Total: accounts.Count,
                NewToday: accounts.Count(a => a.CreatedAt >= today),
                NewLast7Days: accounts.Count(a => a.CreatedAt >= today.AddDays(-7)),
                NewLast30Days: accounts.Count(a => a.CreatedAt >= today.AddDays(-30)));
            return Task.FromResult(stats);
        }
    }

    public Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<RecentLogin> logins = accounts
                .Where(a => a.LastLoginAt != null)
                .OrderByDescending(a => a.LastLoginAt)
                .Take(count)
                .Select(a => new RecentLogin(a.Id, a.DisplayName, a.AvatarUrl != null, a.LastLoginAt!.Value))
                .ToList();
            return Task.FromResult(logins);
        }
    }

    public Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var from = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            IReadOnlyList<DailyRegistration> regs = accounts
                .Where(a => a.CreatedAt >= from)
                .GroupBy(a => DateOnly.FromDateTime(a.CreatedAt))
                .Select(g => new DailyRegistration(g.Key, g.Count()))
                .OrderBy(r => r.Date)
                .ToList();
            return Task.FromResult(regs);
        }
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
