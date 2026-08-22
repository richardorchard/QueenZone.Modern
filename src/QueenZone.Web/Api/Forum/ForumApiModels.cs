namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/forum/categories</c>. Same public fields as the
/// website board cards on <c>/forum</c>.
/// </summary>
public sealed record ForumCategoryListItemDto(
    int Id,
    string Name,
    string? Description,
    int PostCount,
    DateTime? LastActivityAt,
    string? LatestThreadTitle,
    string DetailPath);

/// <summary>
/// Thread row for <c>/api/v1/forum/categories/{id}/topics</c>. Same public fields
/// as the website topic list on a category page.
/// </summary>
public sealed record ForumTopicListItemDto(
    int Id,
    string Title,
    DateTime LastActivityAt,
    string AuthorUsername,
    int ReplyCount,
    string? LastPostUsername,
    bool IsSticky,
    string DetailPath);

/// <summary>
/// Thread header for <c>/api/v1/forum/topics/{id}</c>. Same public fields as the
/// website topic page (title, board, post count).
/// </summary>
public sealed record ForumTopicDetailDto(
    int Id,
    string Title,
    int ForumId,
    string ForumName,
    string CategoryPath,
    string DetailPath,
    int PostCount,
    bool? HasPoll);

/// <summary>
/// Chronological post on <c>/api/v1/forum/topics/{id}/posts</c>. Body is the same
/// sanitized HTML the website renders via <see cref="UgcHtml.FormatForDisplay"/>.
/// </summary>
public sealed record ForumPostDto(
    int Id,
    string Body,
    DateTime PostedAt,
    string AuthorUsername,
    string? Signature,
    DateTime? AuthorMemberSince,
    Guid? AuthorMemberId,
    DateTimeOffset? EditedAt,
    int EditCount,
    IReadOnlyList<ForumAttachmentDto> Attachments);

/// <summary>
/// Attachment card for a forum post. <see cref="Url"/> is the existing
/// member-gated download path (<c>/forum/attachment/...</c>), not a parallel
/// API stream.
/// </summary>
public sealed record ForumAttachmentDto(
    string FileName,
    string Url,
    string Extension,
    string FormattedSize,
    bool IsImage,
    string? ThumbnailUrl);
