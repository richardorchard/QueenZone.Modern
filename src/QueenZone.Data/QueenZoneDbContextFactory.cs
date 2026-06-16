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
            ?? "Server=(localdb)\\mssqllocaldb;Database=QueenZone;Trusted_Connection=True;TrustServerCertificate=True");

        return new QueenZoneDbContext(optionsBuilder.Options);
    }
}