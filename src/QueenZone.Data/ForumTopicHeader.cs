namespace QueenZone.Data;

public sealed record ForumTopicHeader(
    int TopicId,
    string Title,
    int ForumId,
    string ForumName);