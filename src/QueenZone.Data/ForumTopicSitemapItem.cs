namespace QueenZone.Data;

public sealed record ForumTopicSitemapItem(
    int TopicId,
    string Title,
    DateTime? LastActivityAt);