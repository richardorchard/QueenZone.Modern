namespace QueenZone.Data;

public interface IDiscographyRepository
{
    Task<IReadOnlyList<AlbumSummary>> GetAlbumsAsync(CancellationToken cancellationToken = default);

    Task<AlbumDetail?> GetAlbumByIdAsync(int albumId, CancellationToken cancellationToken = default);
}
