namespace QueenZone.Web;

/// <summary>
/// Stable list row for the public biography index.
/// </summary>
public sealed record BiographyChapterSummary(
    int Id,
    string Title,
    string Summary,
    string ChapterNumeral,
    string Marker,
    string DetailPath);

/// <summary>
/// Stable detail shape for a public biography chapter page.
/// </summary>
public sealed record BiographyChapterDetail(
    int Id,
    string Title,
    string Summary,
    string Body,
    string ChapterNumeral,
    string Marker,
    string ReadTimeLabel,
    string DetailPath);

/// <summary>
/// Previous/next chapter links for biography detail navigation.
/// </summary>
public sealed record BiographyChapterNavViewModel(
    BiographyChapterLink? Previous,
    BiographyChapterLink? Next);

/// <summary>
/// Lightweight adjacent-chapter link used on biography detail.
/// </summary>
public sealed record BiographyChapterLink(
    string Title,
    string DetailPath);
