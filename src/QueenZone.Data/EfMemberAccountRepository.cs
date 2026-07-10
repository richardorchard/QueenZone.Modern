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

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
