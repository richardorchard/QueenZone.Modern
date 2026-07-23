using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QueenZone.Data;

public sealed class QueenZoneDbContextFactory : IDesignTimeDbContextFactory<QueenZoneDbContext>
{
    public QueenZoneDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QueenZoneDbContext>();
        optionsBuilder.UseSqlServer(
            Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy")
            ?? "Server=(localdb)\\mssqllocaldb;Database=QueenZone;Trusted_Connection=True;TrustServerCertificate=True",
            sql =>
            {
                sql.CommandTimeout(QueenZoneSqlServerOptions.LongRunningCommandTimeoutSeconds);
                sql.EnableRetryOnFailure(
                    maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                    maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                    errorNumbersToAdd: null);
            });

        return new QueenZoneDbContext(optionsBuilder.Options);
    }
}
