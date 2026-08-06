using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.E2E;

/// <summary>
/// Direct-EF access to the SQL Express mirror for RealData write fixtures that need to seed a
/// member row before impersonating it (<c>X-Test-Member-Id</c>) and to delete the rows they
/// created during teardown. Mirrors <c>EfContentSubmissionLiveProbeTests.CreateContext</c> in
/// <c>QueenZone.Web.Tests</c>. <see cref="RealDataPageTest"/> already guarantees this only runs
/// for write-capable fixtures, and <c>Run-E2E.ps1 -Mode RealData</c> requires
/// <c>ConnectionStrings__QueenZoneLegacy</c> to be set before invoking <c>dotnet test</c>.
/// </summary>
internal static class RealDataDb
{
    public static QueenZoneDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__QueenZoneLegacy is required for RealData write fixtures. " +
                "Run via scripts/Run-E2E.ps1 -Mode RealData.");
        }

        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                        maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                        errorNumbersToAdd: null);
                })
            .Options;
        return new QueenZoneDbContext(options);
    }
}
