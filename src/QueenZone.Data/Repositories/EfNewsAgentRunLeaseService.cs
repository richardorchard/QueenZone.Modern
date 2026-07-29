using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfNewsAgentRunLeaseService(QueenZoneDbContext dbContext) : INewsAgentRunLeaseService
{
    public async Task<INewsAgentRunLease?> TryAcquireAsync(
        string leaseName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var holderId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var expiresAtUtc = now.Add(duration);

        var updated = await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE NewsAgentRunLeases
SET HolderId = {holderId}, AcquiredAtUtc = {now}, ExpiresAtUtc = {expiresAtUtc}
WHERE LeaseName = {leaseName}
  AND (ExpiresAtUtc <= {now} OR HolderId = {holderId})",
            cancellationToken);

        if (updated > 0)
        {
            return new EfNewsAgentRunLease(this, leaseName, holderId);
        }

        var leaseExists = await dbContext.NewsAgentRunLeases
            .AnyAsync(lease => lease.LeaseName == leaseName, cancellationToken);
        if (leaseExists)
        {
            return null;
        }

        try
        {
            dbContext.NewsAgentRunLeases.Add(new NewsAgentRunLeaseEntity
            {
                LeaseName = leaseName,
                HolderId = holderId,
                AcquiredAtUtc = now,
                ExpiresAtUtc = expiresAtUtc
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return new EfNewsAgentRunLease(this, leaseName, holderId);
        }
        catch (DbUpdateException)
        {
            return null;
        }
    }

    internal async Task ReleaseAsync(string leaseName, string holderId, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE NewsAgentRunLeases
SET ExpiresAtUtc = {DateTime.UtcNow}
WHERE LeaseName = {leaseName} AND HolderId = {holderId}",
            cancellationToken);
    }

    private sealed class EfNewsAgentRunLease(
        EfNewsAgentRunLeaseService service,
        string leaseName,
        string holderId) : INewsAgentRunLease
    {
        public string LeaseName { get; } = leaseName;

        public string HolderId { get; } = holderId;

        public async ValueTask DisposeAsync()
        {
            await service.ReleaseAsync(LeaseName, HolderId);
        }
    }
}
