using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Attaches batched discussion fields to news list/detail shapes. Does not render UI.
/// </summary>
public sealed class NewsDiscussionComposer(INewsForumDiscussionLookup discussionLookup)
{
    public async Task<IReadOnlyList<NewsListItemDto>> ToListItemsAsync(
        IReadOnlyList<NewsItem> items,
        CancellationToken cancellationToken = default)
    {
        var counts = await GetReplyCountsAsync(items, cancellationToken);
        return items.Select(item => ContentApiMapper.ToNewsListItem(item, counts)).ToList();
    }

    public async Task<NewsDetailDto> ToDetailAsync(
        NewsItem item,
        CancellationToken cancellationToken = default)
    {
        var discussion = await GetDetailDiscussionAsync(item.ForumTopicId, cancellationToken);
        return ContentApiMapper.ToNewsDetail(item, discussion.ReplyCount, discussion.Preview);
    }

    public async Task<IReadOnlyList<NewsArchiveItem>> ToArchiveItemsAsync(
        IReadOnlyList<NewsItem> items,
        CancellationToken cancellationToken = default)
    {
        var counts = await GetReplyCountsAsync(items, cancellationToken);
        return items.Select(item => PublicContentMapper.ToNewsArchiveItem(item, counts)).ToList();
    }

    public async Task<NewsDetailItem> ToDetailItemAsync(
        NewsItem item,
        CancellationToken cancellationToken = default)
    {
        var discussion = await GetDetailDiscussionAsync(item.ForumTopicId, cancellationToken);
        return PublicContentMapper.ToNewsDetailItem(item, discussion.ReplyCount, discussion.Preview);
    }

    private async Task<IReadOnlyDictionary<int, int>> GetReplyCountsAsync(
        IReadOnlyList<NewsItem> items,
        CancellationToken cancellationToken)
    {
        var topicIds = items
            .Select(item => item.ForumTopicId)
            .OfType<int>()
            .Distinct()
            .ToList();
        if (topicIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await discussionLookup.GetReplyCountsAsync(topicIds, cancellationToken);
    }

    private async Task<(int? ReplyCount, IReadOnlyList<NewsDiscussionPreviewDto>? Preview)> GetDetailDiscussionAsync(
        int? topicId,
        CancellationToken cancellationToken)
    {
        if (topicId is not int id)
        {
            return (null, null);
        }

        var discussion = await discussionLookup.GetDiscussionAsync(
            id,
            NewsForumDiscussion.PreviewReplyCount,
            cancellationToken);
        var preview = discussion.Preview
            .Select(item => new NewsDiscussionPreviewDto(item.AuthorDisplayName, item.PostedAt, item.Excerpt))
            .ToList();
        return (discussion.ReplyCount, preview);
    }
}
