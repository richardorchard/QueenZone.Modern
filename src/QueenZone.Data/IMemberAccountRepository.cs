using QueenZone.Data.Entities;

namespace QueenZone.Data;

public interface IMemberAccountRepository
{
    Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default);

    Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default);
}
