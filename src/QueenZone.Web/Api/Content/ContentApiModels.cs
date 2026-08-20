namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/content/biography</c>.
/// </summary>
public sealed record BiographyChapterListItemDto(
    int Id,
    string Title,
    string Summary,
    int DisplaySequence,
    string DetailPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/biography/{id}</c>, including adjacent-chapter
/// links so the app can render prev/next navigation without a second round trip.
/// </summary>
public sealed record BiographyChapterDetailDto(
    int Id,
    string Title,
    string Summary,
    string Body,
    int DisplaySequence,
    string DetailPath,
    BiographyChapterNavDto? Previous,
    BiographyChapterNavDto? Next);

/// <summary>
/// Minimal reference to an adjacent chapter for prev/next navigation.
/// </summary>
public sealed record BiographyChapterNavDto(int Id, string Title, string DetailPath);
