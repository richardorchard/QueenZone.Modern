using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy studio-album catalogue via its original stored procedures
/// (<c>Q_ALBUM_LIST_SP</c>, <c>Q_ALBUM_T_DISPLAY_SP</c>, <c>Q_ALBUM_SONG_T_LIST_SP</c>),
/// invoked through EF Core rather than Dapper.
/// </summary>
public sealed class EfDiscographyRepository : IDiscographyRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly string listSql;
    private readonly Func<int, FormattableString> displaySql;
    private readonly Func<int, FormattableString> songsSql;

    [ExcludeFromCodeCoverage]
    public EfDiscographyRepository(QueenZoneDbContext dbContext)
        : this(
            dbContext,
            listSql: EfProductionSql.CreateDiscographyQueries().ListSql,
            displaySql: EfProductionSql.CreateDiscographyQueries().DisplaySql,
            songsSql: EfProductionSql.CreateDiscographyQueries().SongsSql)
    {
    }

    internal EfDiscographyRepository(
        QueenZoneDbContext dbContext,
        string listSql,
        Func<int, FormattableString> displaySql,
        Func<int, FormattableString> songsSql)
    {
        this.dbContext = dbContext;
        this.listSql = listSql;
        this.displaySql = displaySql;
        this.songsSql = songsSql;
    }

    public async Task<IReadOnlyList<AlbumSummary>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<AlbumListRow>(listSql)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new AlbumSummary(
                AlbumId: row.Q_ALBUM_ID,
                Name: row.ALBUM_NAME,
                Slug: NewsSlug.Slugify(row.ALBUM_NAME),
                ReleaseYear: row.release_year,
                ThumbnailUrl: AlbumCoverUrl.Build(row.thumb_url)))
            .ToList();
    }

    public async Task<AlbumDetail?> GetAlbumByIdAsync(int albumId, CancellationToken cancellationToken = default)
    {
        var albumRows = await dbContext.Database
            .SqlQuery<AlbumDisplayRow>(displaySql(albumId))
            .ToListAsync(cancellationToken);

        var album = albumRows.FirstOrDefault();

        // Q_ALBUM_T_DISPLAY_SP does not filter on ACTIVE itself, so apply the visibility
        // gate here (mirrors the DISPLAY=1 pattern used for fan performances).
        if (album is null || album.ACTIVE != 1)
        {
            return null;
        }

        var songRows = await dbContext.Database
            .SqlQuery<AlbumSongRow>(songsSql(albumId))
            .ToListAsync(cancellationToken);

        var songs = songRows
            .Select(row => new AlbumSong(
                row.Q_ALBUM_SONG_ID,
                row.SONG_TITLE,
                row.IS_SINGLE == 1,
                string.IsNullOrWhiteSpace(row.SONG_LYRICS) ? null : row.SONG_LYRICS,
                string.IsNullOrWhiteSpace(row.SONG_NOTES) ? null : row.SONG_NOTES))
            .ToList();

        return new AlbumDetail(
            AlbumId: album.Q_ALBUM_ID,
            Name: album.ALBUM_NAME,
            Slug: NewsSlug.Slugify(album.ALBUM_NAME),
            ReleaseYear: album.RELEASE_DATE?.Year,
            ArtistName: album.ARTIST_NAME,
            GeneralNotes: string.IsNullOrWhiteSpace(album.GENERAL_NOTES) ? null : album.GENERAL_NOTES,
            CoverUrl: AlbumCoverUrl.Build(album.PICTURE_URL) ?? AlbumCoverUrl.Build(album.THUMB_URL),
            Songs: songs);
    }

    internal sealed class AlbumListRow
    {
        public int Q_ALBUM_ID { get; set; }

        public string ALBUM_NAME { get; set; } = string.Empty;

        public int? release_year { get; set; }

        public string? thumb_url { get; set; }
    }

    internal sealed class AlbumDisplayRow
    {
        public int Q_ALBUM_ID { get; set; }

        public string ALBUM_NAME { get; set; } = string.Empty;

        public DateTime? RELEASE_DATE { get; set; }

        public string? GENERAL_NOTES { get; set; }

        public string ARTIST_NAME { get; set; } = string.Empty;

        public string? THUMB_URL { get; set; }

        public string? PICTURE_URL { get; set; }

        public int ACTIVE { get; set; }
    }

    internal sealed class AlbumSongRow
    {
        public int Q_ALBUM_SONG_ID { get; set; }

        public string SONG_TITLE { get; set; } = string.Empty;

        public int IS_SINGLE { get; set; }

        public string? SONG_LYRICS { get; set; }

        public string? SONG_NOTES { get; set; }
    }
}
