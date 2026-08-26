using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfDeviceTokenRepository(QueenZoneDbContext dbContext) : IDeviceTokenRepository
{
    public async Task<DeviceTokenEntity> UpsertAsync(
        DeviceTokenEntity token,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.DeviceTokens
            .SingleOrDefaultAsync(row => row.DeviceId == token.DeviceId, cancellationToken);

        if (existing is null)
        {
            dbContext.DeviceTokens.Add(token);
            await dbContext.SaveChangesAsync(cancellationToken);
            return token;
        }

        existing.MemberAccountId = token.MemberAccountId;
        existing.Platform = token.Platform;
        existing.Token = token.Token;
        existing.UpdatedAt = token.UpdatedAt;
        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteByDeviceIdAsync(
        Guid memberAccountId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.DeviceTokens
            .Where(row => row.DeviceId == deviceId && row.MemberAccountId == memberAccountId)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted == 1;
    }

    public async Task<IReadOnlyList<DeviceTokenEntity>> ListByMemberIdsAsync(
        IReadOnlyCollection<Guid> memberAccountIds,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountIds.Count == 0)
        {
            return [];
        }

        var ids = memberAccountIds.Distinct().ToArray();
        return await dbContext.DeviceTokens
            .AsNoTracking()
            .Where(row => ids.Contains(row.MemberAccountId))
            .ToListAsync(cancellationToken);
    }
}
