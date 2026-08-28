using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Durable QueenZone system member used as the News-forum opening-post author (ADR 0016).
/// Not the Entra editor.
/// </summary>
public sealed class NewsForumOptions
{
    public const string SectionName = "NewsForum";

    public string SystemMemberEmail { get; set; } = NewsForumDiscussion.SystemMemberEmail;

    public string SystemMemberDisplayName { get; set; } = NewsForumDiscussion.SystemMemberDisplayName;
}
