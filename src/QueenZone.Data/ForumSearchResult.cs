namespace QueenZone.Data;

public sealed record ForumSearchResult(
    int TopicId,
    string Title,
    int CategoryId,
    string CategoryName,
    int ReplyCount,
    DateTime? LastActivityAt,
    string? StartedByDisplayName);
