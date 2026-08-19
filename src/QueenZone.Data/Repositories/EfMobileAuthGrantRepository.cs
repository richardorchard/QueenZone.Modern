using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfMobileAuthGrantRepository(QueenZoneDbContext dbContext) : IMobileAuthGrantRepository
{
    public async Task StoreAuthorizationCodeAsync(
        MobileAuthAuthorizationCodeEntity code,
        CancellationToken cancellationToken = default)
    {
        dbContext.MobileAuthAuthorizationCodes.Add(code);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MobileAuthAuthorizationCodeEntity?> RedeemAuthorizationCodeAsync(
        string codeHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.MobileAuthAuthorizationCodes
            .Where(code => code.CodeHash == codeHash && code.RedeemedAt == null && code.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(code => code.RedeemedAt, utcNow),
                cancellationToken);

        if (updated != 1)
        {
            return null;
        }

        return await dbContext.MobileAuthAuthorizationCodes
            .AsNoTracking()
            .SingleAsync(code => code.CodeHash == codeHash, cancellationToken);
    }

    public async Task StoreRefreshTokenAsync(
        MobileAuthRefreshTokenEntity token,
        CancellationToken cancellationToken = default)
    {
        dbContext.MobileAuthRefreshTokens.Add(token);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
