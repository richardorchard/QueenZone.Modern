using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps repository DTOs directly to <c>/api/v1/content</c> JSON shapes.
/// Kept separate from <see cref="PublicContentMapper"/>, which serves Razor Pages.
/// </summary>
public static class ContentApiMapper
{
    public static NewsListItemDto ToNewsListItem(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.PublishedAt,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static IReadOnlyList<NewsListItemDto> ToNewsListItems(IEnumerable<NewsItem> items) =>
        items.Select(ToNewsListItem).ToList();

    public static NewsDetailDto ToNewsDetail(NewsItem item) =>
        new(
            item.Id,
            item.Title,
            item.Excerpt,
            item.Body,
            item.PublishedAt,
            item.SourceUrl,
            NewsRoutes.GetNewsDetailPath(item.Id, item.Title, item.Slug));

    public static FreddieTributeDto ToFreddieTributeDto(FreddieTribute tribute) =>
        new(
            tribute.Id,
            tribute.Name,
            tribute.Thought,
            tribute.Country,
            tribute.DateText,
            tribute.TimeText);

    public static IReadOnlyList<FreddieTributeDto> ToFreddieTributeDtos(IEnumerable<FreddieTribute> tributes) =>
        tributes.Select(ToFreddieTributeDto).ToList();
}
