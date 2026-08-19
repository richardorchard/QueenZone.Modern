using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Opaque refresh token issued to a mobile client. The raw token is never stored;
/// only <see cref="TokenHash"/> is persisted so the value can be revoked later.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MobileAuthRefreshTokenEntity
{
    public Guid Id { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public Guid MemberAccountId { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public MemberAccount? MemberAccount { get; set; }
}
