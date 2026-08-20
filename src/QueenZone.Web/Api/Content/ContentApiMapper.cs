using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps repository DTOs directly to <c>/api/v1/content</c> JSON shapes.
/// Kept separate from <see cref="PublicContentMapper"/>, which serves Razor Pages.
/// </summary>
public static class ContentApiMapper
{
    public static BiographyChapterListItemDto ToBiographyChapterListItem(BiographyChapterItem chapter) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            chapter.DisplaySequence,
            BiographyRoutes.GetChapterDetailPath(chapter));

    public static IReadOnlyList<BiographyChapterListItemDto> ToBiographyChapterListItems(
        IEnumerable<BiographyChapterItem> chapters) =>
        chapters.Select(ToBiographyChapterListItem).ToList();

    public static BiographyChapterDetailDto ToBiographyChapterDetail(
        BiographyChapterItem chapter,
        BiographyChapterNav navigation) =>
        new(
            chapter.Id,
            chapter.Title,
            BiographyContent.GetListSummary(chapter),
            chapter.Body,
            chapter.DisplaySequence,
            BiographyRoutes.GetChapterDetailPath(chapter),
            ToBiographyChapterNavDto(navigation.Previous),
            ToBiographyChapterNavDto(navigation.Next));

    private static BiographyChapterNavDto? ToBiographyChapterNavDto(BiographyChapterItem? chapter) =>
        chapter is null
            ? null
            : new BiographyChapterNavDto(chapter.Id, chapter.Title, BiographyRoutes.GetChapterDetailPath(chapter));
}
