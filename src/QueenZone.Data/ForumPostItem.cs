namespace QueenZone.Data;

public sealed record ForumPostItem(
    int Id,
    string Body,
    DateTime PostedAt,
    string AuthorUsername,
    string? Signature,
    int AuthorPostCount,
    DateTime? AuthorMemberSince);