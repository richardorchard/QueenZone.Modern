using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyBiographyRepository(string connectionString) : IBiographyRepository
{
    public async Task<IReadOnlyList<BiographyChapterItem>> GetChaptersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "Q_BIO_LIST_SP",
            commandType: System.Data.CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ListRow>(command);
        return rows.Select(MapListRow).ToList();
    }

    public async Task<BiographyChapterItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "Q_BIO_DISPLAY_SP",
            new { Q_BIO_ID = (short)id },
            commandType: System.Data.CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<DetailRow>(command);
        return row is null ? null : MapDetailRow(id, row);
    }

    public async Task<BiographyChapterNav> GetAdjacentChaptersAsync(int id, CancellationToken cancellationToken = default)
    {
        var chapters = BiographyChapterOrdering.ByDisplaySequenceAscending(
            await GetChaptersAsync(cancellationToken));
        var index = chapters.ToList().FindIndex(chapter => chapter.Id == id);
        if (index < 0)
        {
            return new BiographyChapterNav(null, null);
        }

        var previous = index > 0 ? chapters[index - 1] : null;
        var next = index < chapters.Count - 1 ? chapters[index + 1] : null;
        return new BiographyChapterNav(previous, next);
    }

    private static BiographyChapterItem MapListRow(ListRow row) =>
        new(
            row.Q_BIO_ID,
            row.TITLE?.Trim() ?? string.Empty,
            ResolveSummary(row.SUMMARY),
            string.Empty,
            row.DISPLAY_SEQUENCE,
            row.CREATE_DATE);

    private static BiographyChapterItem MapDetailRow(int id, DetailRow row) =>
        new(
            id,
            row.TITLE?.Trim() ?? string.Empty,
            ResolveSummary(row.SUMMARY, row.BIO_TEXT),
            row.BIO_TEXT ?? string.Empty,
            row.DISPLAY_SEQUENCE,
            DateTime.MinValue);

    private static string ResolveSummary(string? summary, string? body = null)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary.Trim();
        }

        return LegacyArticleText.GetExcerpt(body);
    }

    private sealed class ListRow
    {
        public string? BIOTITLE { get; init; }

        public short Q_BIO_ID { get; init; }

        public DateTime CREATE_DATE { get; init; }

        public string? TITLE { get; init; }

        public byte DISPLAY_SEQUENCE { get; init; }

        public string? SUMMARY { get; init; }
    }

    private sealed class DetailRow
    {
        public string? TITLE { get; init; }

        public string? SUMMARY { get; init; }

        public string? BIO_TEXT { get; init; }

        public byte DISPLAY_SEQUENCE { get; init; }
    }
}
