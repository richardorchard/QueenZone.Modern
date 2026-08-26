namespace QueenZone.Web;

/// <summary>
/// Watch state for <c>/api/v1/forum/topics/{id}/watch</c>. Watching is the
/// deliberate opt-in for forum reply pushes; <c>forumReply</c> preference is a
/// master mute only.
/// </summary>
public sealed record ForumTopicWatchDto(bool Watching);
