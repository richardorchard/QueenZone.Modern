using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryTopicWatchRepository : ITopicWatchRepository
{
    private readonly List<MemberTopicWatchEntity> watches = [];
    private readonly Lock gate = new();

    public Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<Guid> ids = watches
                .Where(watch => watch.TopicId == topicId)
                .Select(watch => watch.MemberAccountId)
                .Distinct()
                .ToArray();
            return Task.FromResult(ids);
        }
    }

    public Task<bool> IsWatchingAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            return Task.FromResult(watches.Any(
                watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId));
        }
    }

    public Task WatchAsync(
        Guid memberAccountId,
        int topicId,
        DateTimeOffset watchedAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (watches.Any(
                    watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId))
            {
                return Task.CompletedTask;
            }

            watches.Add(new MemberTopicWatchEntity
            {
                MemberAccountId = memberAccountId,
                TopicId = topicId,
                CreatedAt = watchedAt,
            });
            return Task.CompletedTask;
        }
    }

    public Task<bool> UnwatchAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var removed = watches.RemoveAll(
                watch => watch.MemberAccountId == memberAccountId && watch.TopicId == topicId);
            return Task.FromResult(removed > 0);
        }
    }
}
