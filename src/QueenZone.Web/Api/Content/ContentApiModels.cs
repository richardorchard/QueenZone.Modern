namespace QueenZone.Web;

/// <summary>
/// List-card shape for <c>/api/v1/content/news</c>. Deliberately decoupled from
/// <see cref="NewsArchiveItem"/> so changes to the Razor Pages view model do not
/// silently change the mobile JSON contract.
/// </summary>
public sealed record NewsListItemDto(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string DetailPath);

/// <summary>
/// Detail shape for <c>/api/v1/content/news/{id}</c>.
/// </summary>
public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    string DetailPath);

/// <summary>
/// List-card shape for <c>/api/v1/content/freddietribute</c>. No detail endpoint: the
/// website has no single-tribute page, only the paged tribute archive.
/// </summary>
public sealed record FreddieTributeDto(
    int Id,
    string Name,
    string Thought,
    string? Country,
    string DateText,
    string? TimeText);
