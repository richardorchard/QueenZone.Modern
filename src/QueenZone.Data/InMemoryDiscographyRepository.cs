namespace QueenZone.Data;

public sealed class InMemoryDiscographyRepository : IDiscographyRepository
{
    private readonly IReadOnlyList<AlbumSeed> seedAlbums;

    public InMemoryDiscographyRepository(IReadOnlyList<AlbumSeed> seedAlbums)
    {
        this.seedAlbums = seedAlbums;
    }

    public Task<IReadOnlyList<AlbumSummary>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AlbumSummary> albums = seedAlbums
            .OrderBy(seed => seed.ReleaseYear)
            .Select(seed => new AlbumSummary(
                AlbumId: seed.AlbumId,
                Name: seed.Name,
                Slug: NewsSlug.Slugify(seed.Name),
                ReleaseYear: seed.ReleaseYear,
                ThumbnailUrl: AlbumCoverUrl.Build($"{NewsSlug.Slugify(seed.Name)}-thumb.jpg")))
            .ToList();

        return Task.FromResult(albums);
    }

    public Task<AlbumDetail?> GetAlbumByIdAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var seed = seedAlbums.FirstOrDefault(s => s.AlbumId == albumId);
        if (seed is null)
        {
            return Task.FromResult<AlbumDetail?>(null);
        }

        var slug = NewsSlug.Slugify(seed.Name);
        var songs = seed.SongTitles
            .Select((title, index) => new AlbumSong(
                SongId: (seed.AlbumId * 1000) + index + 1,
                Title: title,
                IsSingle: false,
                Lyrics: null,
                Notes: null))
            .ToList();

        var detail = new AlbumDetail(
            AlbumId: seed.AlbumId,
            Name: seed.Name,
            Slug: slug,
            ReleaseYear: seed.ReleaseYear,
            ArtistName: "Queen",
            GeneralNotes: seed.GeneralNotes,
            CoverUrl: AlbumCoverUrl.Build($"{slug}-cover.jpg"),
            Songs: songs);

        return Task.FromResult<AlbumDetail?>(detail);
    }
}
