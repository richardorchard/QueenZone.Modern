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
/// List-card shape for <c>/api/v1/content/timeline</c>. No detail endpoint: the website
/// has no single-event page, only the one continuous timeline list.
/// </summary>
public sealed record TimelineEventDto(
    int Id,
    string Title,
    string Summary,
    DateTime EventDate,
    string FormattedDate,
    string Category,
    string CategoryLabel,
    string? SourceUrl);
