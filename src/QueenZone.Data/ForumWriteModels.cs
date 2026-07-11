namespace QueenZone.Data;

public sealed record NewForumThread(
    int CategoryId,
    Guid AuthorMemberId,
    string AuthorDisplayName,
    string Subject,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record NewForumPost(
    int TopicId,
    Guid AuthorMemberId,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record ForumWriteThread(
    int TopicId,
    int CategoryId,
    string Subject,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastPostAt,
    int PostCount,
    bool IsLocked);

public sealed record ForumThreadCreateResult(int TopicId, int StarterPostId);
