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

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
