using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class QueenZoneSqlServerOptionsTests
{
    [Fact]
    public void Runtime_defaults_are_shorter_than_legacy_300s_timeout()
    {
        Assert.True(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds < 300);
        Assert.Equal(30, QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
        Assert.Equal(300, QueenZoneSqlServerOptions.LongRunningCommandTimeoutSeconds);
        Assert.True(QueenZoneSqlServerOptions.MaxRetryCount > 0);
        Assert.True(QueenZoneSqlServerOptions.MaxRetryDelaySeconds > 0);
    }

    [Fact]
    public void Runtime_registration_applies_default_command_timeout()
    {
        var builder = new DbContextOptionsBuilder<QueenZoneDbContext>();
        builder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=QueenZoneOptionsTest;Trusted_Connection=True;TrustServerCertificate=True",
            sql =>
            {
                sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                sql.EnableRetryOnFailure(
                    maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                    maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                    errorNumbersToAdd: null);
            });

        using var context = new QueenZoneDbContext(builder.Options);
        Assert.Equal(
            QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds,
            context.Database.GetCommandTimeout());
    }
}
