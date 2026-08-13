namespace QueenZone.Data;

/// <summary>
/// Cross-board recent thread row for the public forum index activity feed.
/// </summary>
public sealed record ForumRecentThreadItem(
    int TopicId,
    string Title,
    int CategoryId,
    string CategoryName,
    int ReplyCount,
    DateTime LastActivityAt);
