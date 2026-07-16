namespace QueenZone.Data;

public sealed record ForumPostItem(
    int Id,
    string Body,
    DateTime PostedAt,
    string AuthorUsername,
    string? Signature,
    int AuthorPostCount,
    DateTime? AuthorMemberSince,
    IReadOnlyList<ForumPostAttachment>? Attachments = null,
    Guid? AuthorMemberId = null,
    DateTimeOffset? EditedAt = null,
    int EditCount = 0);