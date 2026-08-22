using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps repository DTOs to <c>/api/v1/forum</c> JSON shapes.
/// Paths reuse <see cref="ForumRoutes"/> so mobile links match the website.
/// </summary>
public static class ForumApiMapper
{
    public static ForumCategoryListItemDto ToCategoryListItem(ForumCategoryItem category) =>
        new(
            category.Id,
            category.Name,
            category.Description,
            category.PostCount,
            category.LastActivityAt,
            category.LatestThreadTitle,
            ForumRoutes.GetCategoryCanonicalPath(category.Id, category.Name));

    public static IReadOnlyList<ForumCategoryListItemDto> ToCategoryListItems(
        IEnumerable<ForumCategoryItem> categories) =>
        categories.Select(ToCategoryListItem).ToList();

    public static ForumTopicListItemDto ToTopicListItem(ForumTopicItem topic) =>
        new(
            topic.Id,
            topic.Title,
            topic.LastActivityAt,
            topic.AuthorUsername,
            topic.ReplyCount,
            topic.LastPostUsername,
            topic.IsSticky,
            ForumRoutes.GetTopicCanonicalPath(topic.Id, topic.Title));

    public static IReadOnlyList<ForumTopicListItemDto> ToTopicListItems(
        IEnumerable<ForumTopicItem> topics) =>
        topics.Select(ToTopicListItem).ToList();

    public static ForumTopicDetailDto ToTopicDetail(
        ForumTopicHeader header,
        int postCount,
        bool isLocked = false)
    {
        var view = PublicContentMapper.ToForumThreadHeader(header);
        return new ForumTopicDetailDto(
            view.TopicId,
            view.Title,
            view.ForumId,
            view.ForumName,
            view.CategoryPath,
            view.DetailPath,
            postCount,
            view.HasPoll,
            isLocked);
    }

    public static ForumPostDto ToPost(ForumPostItem post, UgcHtml ugcHtml)
    {
        var attachments = PublicContentMapper.ToForumPostViewModel(post).Attachments
            .Select(attachment => new ForumAttachmentDto(
                attachment.FileName,
                attachment.Url,
                attachment.Extension,
                attachment.FormattedSize,
                attachment.IsImage,
                attachment.ThumbnailUrl))
            .ToList();

        return new ForumPostDto(
            post.Id,
            ugcHtml.FormatForDisplay(post.Body),
            post.PostedAt,
            post.AuthorUsername,
            post.Signature,
            post.AuthorMemberSince,
            post.AuthorMemberId,
            post.EditedAt,
            post.EditCount,
            attachments);
    }

    public static IReadOnlyList<ForumPostDto> ToPosts(
        IEnumerable<ForumPostItem> posts,
        UgcHtml ugcHtml) =>
        posts.Select(post => ToPost(post, ugcHtml)).ToList();

    public static ForumPollDto ToPoll(ForumPollResults poll) =>
        new(
            poll.PollId,
            poll.LegacyTopicId,
            poll.Question,
            poll.IsMultiChoice,
            poll.MaxChoices,
            poll.ClosesAt,
            poll.ClosedAt,
            poll.CreatedAt,
            poll.TotalVotes,
            poll.DistinctVoters,
            poll.ViewerHasVoted,
            poll.IsClosed,
            poll.CanViewerVote,
            poll.CanViewerClose,
            poll.Options.Select(ToPollOption).ToList());

    public static ForumPollOptionDto ToPollOption(ForumPollOptionResult option) =>
        new(
            option.OptionId,
            option.OptionText,
            option.DisplayOrder,
            option.VoteCount,
            option.Percentage,
            option.SelectedByViewer);
}
