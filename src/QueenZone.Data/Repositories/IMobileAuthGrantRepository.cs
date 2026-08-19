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
}
