using QueenZone.Data;

namespace QueenZone.Web;

public sealed class TopicWatchService(
    IForumRepository forumRepository,
    ITopicWatchRepository topicWatchRepository,
    TimeProvider timeProvider)
{
    public async Task<TopicWatchStatus?> GetStatusAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        if (!await TopicExistsAsync(topicId, cancellationToken))
        {
            return null;
        }

        var watching = await topicWatchRepository.IsWatchingAsync(
            memberAccountId,
            topicId,
            cancellationToken);
        return new TopicWatchStatus(watching);
    }

    public async Task<TopicWatchStatus?> WatchAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        if (!await TopicExistsAsync(topicId, cancellationToken))
        {
            return null;
        }

        await topicWatchRepository.WatchAsync(
            memberAccountId,
            topicId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        return new TopicWatchStatus(true);
    }

    public async Task<TopicWatchStatus?> UnwatchAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default)
    {
        if (!await TopicExistsAsync(topicId, cancellationToken))
        {
            return null;
        }

        await topicWatchRepository.UnwatchAsync(memberAccountId, topicId, cancellationToken);
        return new TopicWatchStatus(false);
    }

    private async Task<bool> TopicExistsAsync(int topicId, CancellationToken cancellationToken)
    {
        var page = await forumRepository.GetTopicPostsPageAsync(topicId, 1, 1, cancellationToken);
        return page is not null;
    }
}

public sealed record TopicWatchStatus(bool Watching);
