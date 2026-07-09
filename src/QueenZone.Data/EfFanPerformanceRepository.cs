using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy fan-stage recordings via <c>Q_STAGE_T_PAGE_SP</c> (DISPLAY=1 + DATE_ADDED DESC)
/// and direct SQL for count/detail, through EF Core rather than Dapper.
/// The proc's <c>@ItemCount</c> output counts every row (not only visible), so it is not trusted
/// for pagination math — <see cref="GetVisibleCountAsync"/> runs a reliable count instead.
/// </summary>
public sealed class EfFanPerformanceRepository : IFanPerformanceRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly bool useLegacyProcedures;
    private readonly string pageSelectSql;
    private readonly string countSql;
    private readonly Func<int, FormattableString> byIdSql;

    [ExcludeFromCodeCoverage] // Production SQL Server wiring; methods covered via test SQL hooks.
    public EfFanPerformanceRepository(QueenZoneDbContext dbContext)
        : this(
            dbContext,
            useLegacyProcedures: true,
            pageSelectSql: string.Empty,
            countSql: "SELECT COUNT(*) AS Value FROM dbo.Q_STAGE_T WHERE DISPLAY = 1",
            byIdSql: static id => $"""
                SELECT Q_STAGE_ID, TITLE, PERFORMED_BY, DESCRIPTION, URL, thesize, DATE_ADDED
                FROM dbo.Q_STAGE_T
                WHERE Q_STAGE_ID = {id} AND DISPLAY = 1
                """)
    {
    }

    internal EfFanPerformanceRepository(
        QueenZoneDbContext dbContext,
        bool useLegacyProcedures,
        string pageSelectSql,
        string countSql,
        Func<int, FormattableString> byIdSql)
    {
        this.dbContext = dbContext;
        this.useLegacyProcedures = useLegacyProcedures;
        this.pageSelectSql = pageSelectSql;
        this.countSql = countSql;
        this.byIdSql = byIdSql;
    }

    public async Task<IReadOnlyList<FanPerformance>> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (useLegacyProcedures)
        {
            return await GetPageViaProcedureAsync(page, pageSize, cancellationToken);
        }

        var rowsFromSql = await dbContext.Database
            .SqlQueryRaw<StageRow>(pageSelectSql)
            .ToListAsync(cancellationToken);
        return rowsFromSql.Select(MapRow).ToList();
    }

    [ExcludeFromCodeCoverage] // SQL Server stored procedure path.
    private async Task<IReadOnlyList<FanPerformance>> GetPageViaProcedureAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var currentPage = Math.Max(page, 1);
        var rows = await EfSql.QueryProcAsync<StageRow>(
            dbContext,
            "Q_STAGE_T_PAGE_SP",
            command =>
            {
                command.Parameters.Add(EfSql.Input("@PageSize", pageSize));
                command.Parameters.Add(EfSql.Input("@CurrentPage", currentPage));
                command.Parameters.Add(EfSql.OutputInt("@ItemCount"));
            },
            cancellationToken: cancellationToken);

        return rows.Select(MapRow).ToList();
    }

    public async Task<int> GetVisibleCountAsync(CancellationToken cancellationToken = default)
    {
        if (useLegacyProcedures &&
            string.Equals(
                dbContext.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.SqlServer",
                StringComparison.Ordinal))
        {
            return await GetVisibleCountViaSqlServerAsync(cancellationToken);
        }

        return await dbContext.Database
            .SqlQueryRaw<int>(countSql)
            .FirstAsync(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private Task<int> GetVisibleCountViaSqlServerAsync(CancellationToken cancellationToken) =>
        EfSql.ExecuteScalarSqlAsync(
            dbContext,
            "SELECT COUNT(*) FROM dbo.Q_STAGE_T WHERE DISPLAY = 1",
            cancellationToken: cancellationToken);

    public async Task<FanPerformance?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQuery<StageRow>(byIdSql(id))
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
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

    internal sealed class StageRow
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
