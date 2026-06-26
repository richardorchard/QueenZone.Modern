namespace QueenZone.Data;

public static class BiographyChapterOrdering
{
    public static IReadOnlyList<BiographyChapterItem> ByDisplaySequenceAscending(
        IEnumerable<BiographyChapterItem> chapters) =>
        chapters
            .OrderBy(chapter => chapter.DisplaySequence)
            .ThenBy(chapter => chapter.Id)
            .ToList();

    public static IReadOnlyList<BiographyChapterItem> ByDisplaySequenceDescending(
        IEnumerable<BiographyChapterItem> chapters) =>
        chapters
            .OrderByDescending(chapter => chapter.DisplaySequence)
            .ThenByDescending(chapter => chapter.Id)
            .ToList();
}