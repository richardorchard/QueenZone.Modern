namespace QueenZone.Data;

public interface IBiographyRepository
{
    Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default);

    Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(AdminBiographyDraft draft, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, AdminBiographyDraft draft, CancellationToken cancellationToken = default);
}
