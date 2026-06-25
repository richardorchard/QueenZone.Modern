namespace QueenZone.Data;

public static class BiographyChapterOrdering
{
    public static IReadOnlyList<BiographyChapterItem> ByDisplaySequenceAscending(
        IEnumerable<BiographyChapterItem> chapters) =>
        chapters
            .OrderBy(chapter => chapter.DisplaySequence)
            .ThenBy(chapter => chapter.Id)
            .ToList();
}