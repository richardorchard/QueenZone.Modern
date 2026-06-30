using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy studio-album catalogue via its original stored procedures
/// (Q_ALBUM_LIST_SP, Q_ALBUM_T_DISPLAY_SP, Q_ALBUM_SONG_T_LIST_SP).
/// </summary>
public sealed class LegacyDiscographyRepository : IDiscographyRepository
{
    private readonly string connectionString;

    public LegacyDiscographyRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<IReadOnlyList<AlbumSummary>> GetAlbumsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "Q_ALBUM_LIST_SP",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<AlbumListRow>(command);

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
        await using var connection = new SqlConnection(connectionString);

        var albumCommand = new CommandDefinition(
            "Q_ALBUM_T_DISPLAY_SP",
            new { Q_ALBUM_ID = albumId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var album = await connection.QuerySingleOrDefaultAsync<AlbumDisplayRow>(albumCommand);

        // Q_ALBUM_T_DISPLAY_SP does not filter on ACTIVE itself, so apply the visibility
        // gate here (mirrors the DISPLAY=1 pattern used for fan performances).
        if (album is null || album.ACTIVE != 1)
        {
            return null;
        }

        var songsCommand = new CommandDefinition(
            "Q_ALBUM_SONG_T_LIST_SP",
            new { Q_ALBUM_ID = albumId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var songRows = await connection.QueryAsync<AlbumSongRow>(songsCommand);

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

    private sealed class AlbumListRow
    {
        public int Q_ALBUM_ID { get; set; }

        public string ALBUM_NAME { get; set; } = string.Empty;

        public int? release_year { get; set; }

        public string? thumb_url { get; set; }
    }

    private sealed class AlbumDisplayRow
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

    private sealed class AlbumSongRow
    {
        public int Q_ALBUM_SONG_ID { get; set; }

        public string SONG_TITLE { get; set; } = string.Empty;

        public int IS_SINGLE { get; set; }

        public string? SONG_LYRICS { get; set; }

        public string? SONG_NOTES { get; set; }
    }
}
