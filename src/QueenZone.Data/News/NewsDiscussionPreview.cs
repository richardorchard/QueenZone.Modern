namespace QueenZone.Data;

public sealed record NewsDiscussionPreview(
    string AuthorDisplayName,
    DateTime PostedAt,
    string Excerpt);
