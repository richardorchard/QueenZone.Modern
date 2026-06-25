namespace QueenZone.Data;

public interface IBiographyRepository
{
    Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default);

    Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default);
}