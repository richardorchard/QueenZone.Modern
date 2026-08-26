using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfTopicWatchRepository(QueenZoneDbContext dbContext) : ITopicWatchRepository
{
    public async Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.MemberTopicWatches
            .AsNoTracking()
            .Where(watch => watch.TopicId == topicId)
            .Select(watch => watch.MemberAccountId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids;
    }

    public Task<bool> IsWatchingAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default) =>
        dbContext.MemberTopicWatches
            .AsNoTracking()
            .AnyAsync(
                watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId,
                cancellationToken);

    public async Task WatchAsync(
        Guid memberAccountId,
        int topicId,
        DateTimeOffset watchedAt,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.MemberTopicWatches.AnyAsync(
            watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.MemberTopicWatches.Add(new MemberTopicWatchEntity
        {
            MemberAccountId = memberAccountId,
            TopicId = topicId,
            CreatedAt = watchedAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UnwatchAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.MemberTopicWatches
            .Where(watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
