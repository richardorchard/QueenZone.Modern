namespace QueenZone.Data;

public sealed class InMemoryBiographyRepository(IReadOnlyList<BiographyChapterItem> seedChapters) : IBiographyRepository
{
    private readonly IReadOnlyList<BiographyChapterItem> listChapters =
        BiographyChapterOrdering.ByDisplaySequenceDescending(seedChapters);

    private readonly IReadOnlyList<BiographyChapterItem> readingOrderChapters =
        BiographyChapterOrdering.ByDisplaySequenceAscending(seedChapters);

    public Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(listChapters);

    public Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(readingOrderChapters.SingleOrDefault(chapter => chapter.Id == id));

    public Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default)
    {
        var index = readingOrderChapters.ToList().FindIndex(chapter => chapter.Id == id);
        if (index < 0)
        {
            return Task.FromResult(new BiographyChapterNav(null, null));
        }

        var previous = index > 0 ? readingOrderChapters[index - 1] : null;
        var next = index < readingOrderChapters.Count - 1 ? readingOrderChapters[index + 1] : null;
        return Task.FromResult(new BiographyChapterNav(previous, next));
    }
}