using QueenZone.Data.Entities;

namespace QueenZone.Data;

public interface IMobileAuthGrantRepository
{
    Task StoreAuthorizationCodeAsync(
        MobileAuthAuthorizationCodeEntity code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically consumes a still-valid authorization code. Returns null when the hash is
    /// unknown, already redeemed, or expired.
    /// </summary>
    Task<MobileAuthAuthorizationCodeEntity?> RedeemAuthorizationCodeAsync(
        string codeHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task StoreRefreshTokenAsync(
        MobileAuthRefreshTokenEntity token,
        CancellationToken cancellationToken = default);

    Task<MobileAuthRefreshTokenEntity?> FindRefreshTokenByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a still-valid refresh token revoked. Returns false when the hash is unknown,
    /// already revoked, or expired.
    /// </summary>
    Task<bool> TryRevokeRefreshTokenAsync(
        string tokenHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllRefreshTokensForMemberAsync(
        Guid memberAccountId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
