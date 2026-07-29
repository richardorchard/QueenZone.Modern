using QueenZone.Data.Entities;

namespace QueenZone.Data;

public interface IMemberAccountRepository
{
    Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default);

    Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default);

    Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates <see cref="MemberAccount.DisplayName"/> for the given member.
    /// Display names are not unique — multiple members may share the same name.
    /// </summary>
    Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or clears <see cref="MemberAccount.AvatarUrl"/> (blob path within ugc-avatars).
    /// Pass null to remove the avatar.
    /// </summary>
    Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default);

    Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default);

    Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default);
}
