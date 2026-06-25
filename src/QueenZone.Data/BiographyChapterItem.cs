namespace QueenZone.Data;

public sealed record BiographyChapterItem(
    int Id,
    string Title,
    string Summary,
    string Body,
    byte DisplaySequence,
    DateTime CreatedAt);

public sealed record BiographyChapterNav(
    BiographyChapterItem? Previous,
    BiographyChapterItem? Next);