namespace QueenZone.Data;

public sealed record ForumTopicHeader(
    int TopicId,
    string Title,
    int ForumId,
    string ForumName,
    /// <summary>
    /// When false, topic pages skip the poll query. Null means unknown (load poll defensively).
    /// </summary>
    bool? HasPoll = null);