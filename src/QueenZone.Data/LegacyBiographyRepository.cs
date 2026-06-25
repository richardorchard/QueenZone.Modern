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
        return BiographyChapterOrdering.ByDisplaySequenceAscending(rows.Select(MapListRow));
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
        var chapters = await GetChaptersAsync(cancellationToken);
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
            ResolveSummary(row.SUMMARY, row.TITLE),
            string.Empty,
            row.DISPLAY_SEQUENCE,
            row.CREATE_DATE);

    private static BiographyChapterItem MapDetailRow(int id, DetailRow row) =>
        new(
            id,
            row.TITLE?.Trim() ?? string.Empty,
            row.SUMMARY?.Trim() ?? string.Empty,
            row.BIO_TEXT ?? string.Empty,
            row.DISPLAY_SEQUENCE,
            DateTime.MinValue);

    private static string ResolveSummary(string? summary, string? title)
    {
        if (!string.IsNullOrWhiteSpace(summary))
        {
            return summary.Trim();
        }

        return title?.Trim() ?? string.Empty;
    }

    private sealed record ListRow(
        int Q_BIO_ID,
        string? TITLE,
        string? SUMMARY,
        byte DISPLAY_SEQUENCE,
        DateTime CREATE_DATE);

    private sealed record DetailRow(
        string? TITLE,
        string? SUMMARY,
        string? BIO_TEXT,
        byte DISPLAY_SEQUENCE);
}