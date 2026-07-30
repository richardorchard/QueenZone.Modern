namespace QueenZone.Data;

public sealed class InMemoryBiographyRepository(SharedBiographyStore store) : IBiographyRepository
{
    public InMemoryBiographyRepository(IReadOnlyList<BiographyChapterItem> seedChapters)
        : this(new SharedBiographyStore(seedChapters))
    {
    }

    public Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(BiographyChapterOrdering.ByDisplaySequenceDescending(store.GetAll()));

    public Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetById(id));

    public Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default)
    {
        var readingOrder = BiographyChapterOrdering.ByDisplaySequenceAscending(store.GetAll());
        var index = readingOrder.ToList().FindIndex(chapter => chapter.Id == id);
        if (index < 0)
        {
            return Task.FromResult(new BiographyChapterNav(null, null));
        }

        var previous = index > 0 ? readingOrder[index - 1] : null;
        var next = index < readingOrder.Count - 1 ? readingOrder[index + 1] : null;
        return Task.FromResult(new BiographyChapterNav(previous, next));
    }

    public Task<int> CreateAsync(AdminBiographyDraft draft, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Create(draft));

    public Task UpdateAsync(int id, AdminBiographyDraft draft, CancellationToken cancellationToken = default)
    {
        if (!store.Update(id, draft))
        {
            throw new InvalidOperationException($"Biography chapter {id} was not found.");
        }

        return Task.CompletedTask;
    }
}
