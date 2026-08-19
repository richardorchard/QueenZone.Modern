using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Single-use authorization code issued after a successful mobile OAuth provider handshake.
/// The raw code is never stored; only <see cref="CodeHash"/> is persisted.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class MobileAuthAuthorizationCodeEntity
{
    public Guid Id { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public Guid MemberAccountId { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public MemberAccount? MemberAccount { get; set; }
}
