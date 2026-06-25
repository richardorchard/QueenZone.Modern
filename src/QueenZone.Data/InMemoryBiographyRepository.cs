namespace QueenZone.Data;

public sealed class InMemoryBiographyRepository(IReadOnlyList<BiographyChapterItem> seedChapters) : IBiographyRepository
{
    private readonly IReadOnlyList<BiographyChapterItem> chapters =
        BiographyChapterOrdering.ByDisplaySequenceAscending(seedChapters);

    public Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(chapters);

    public Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(chapters.SingleOrDefault(chapter => chapter.Id == id));

    public Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default)
    {
        var index = chapters.ToList().FindIndex(chapter => chapter.Id == id);
        if (index < 0)
        {
            return Task.FromResult(new BiographyChapterNav(null, null));
        }

        var previous = index > 0 ? chapters[index - 1] : null;
        var next = index < chapters.Count - 1 ? chapters[index + 1] : null;
        return Task.FromResult(new BiographyChapterNav(previous, next));
    }
}