using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfNewsAuditRepository(QueenZoneDbContext dbContext) : INewsAuditRepository
{
    public async Task AppendAsync(
        int newsId,
        string action,
        string actorEmail,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.NewsAuditLogs.Add(new NewsAuditLogEntity
        {
            NewsId = newsId,
            Action = action,
            ActorEmail = actorEmail,
            Details = details,
            OccurredAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NewsAuditEntry>> GetByNewsIdAsync(int newsId, CancellationToken cancellationToken = default)
    {
        var entries = await dbContext.NewsAuditLogs
            .AsNoTracking()
            .Where(entry => entry.NewsId == newsId)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new NewsAuditEntry(
                entry.Id,
                entry.NewsId,
                entry.Action,
                entry.ActorEmail,
                entry.OccurredAt,
                entry.Details))
            .ToListAsync(cancellationToken);

        return entries;
    }
}