using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy fan-stage recordings via the original Q_STAGE_T_PAGE_SP, which already
/// encodes the DISPLAY=1 visibility rule and DATE_ADDED DESC ordering. That proc's own
/// @ItemCount output counts every Q_STAGE_T row (not just visible ones), so it is not trusted
/// for pagination math here — GetVisibleCountAsync runs a direct, reliable count instead.
/// </summary>
public sealed class LegacyFanPerformanceRepository : IFanPerformanceRepository
{
    private readonly string connectionString;

    public LegacyFanPerformanceRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<IReadOnlyList<FanPerformance>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);

        var parameters = new DynamicParameters();
        parameters.Add("PageSize", pageSize);
        parameters.Add("CurrentPage", Math.Max(page, 1));
        parameters.Add("ItemCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "Q_STAGE_T_PAGE_SP",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<StageRow>(command);
        return rows.Select(MapRow).ToList();
    }

    public async Task<int> GetVisibleCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "SELECT COUNT(*) FROM dbo.Q_STAGE_T WHERE DISPLAY = 1",
            cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<FanPerformance?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            """
            SELECT Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED
            FROM dbo.Q_STAGE_T
            WHERE Q_STAGE_ID = @id AND DISPLAY = 1
            """,
            new { id },
            cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<StageRow>(command);
        return row is null ? null : MapRow(row);
    }

    private static FanPerformance MapRow(StageRow row) => new(
        Id: row.Q_STAGE_ID,
        Title: row.TITLE,
        PerformedBy: row.PERFORMED_BY,
        Description: row.DESCRIPTION ?? string.Empty,
        AudioUrl: SongFileUrl.Build(row.URL),
        FileSizeBytes: ParseFileSize(row.thesize),
        DateAdded: row.DATE_ADDED);

    private static long ParseFileSize(string? thesize) =>
        long.TryParse(thesize, out var parsed) ? parsed : 0;

    private sealed class StageRow
    {
        public int Q_STAGE_ID { get; set; }

        public string TITLE { get; set; } = string.Empty;

        public string PERFORMED_BY { get; set; } = string.Empty;

        public string? DESCRIPTION { get; set; }

        public string URL { get; set; } = string.Empty;

        public string? thesize { get; set; }

        public DateTime DATE_ADDED { get; set; }
    }
}
