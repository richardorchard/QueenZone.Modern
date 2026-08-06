using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.E2E;

/// <summary>
/// Proves the RealData cleanup context uses the long-running command timeout rather than the
/// 30s public-request default, so macOS runners have headroom for LAN round-trips to the
/// glory11 mirror during teardown (#574).
/// </summary>
[TestFixture]
[Category(E2ECategories.Deterministic)]
public class RealDataDbTests
{
    [Test]
    public void CreateContext_UsesLongRunningCommandTimeout()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__QueenZoneLegacy",
            "Server=(localdb)\\mssqllocaldb;Database=RealDataDbTimeoutTest;Trusted_Connection=True;TrustServerCertificate=True");
        try
        {
            using var db = RealDataDb.CreateContext();

            Assert.That(
                db.Database.GetCommandTimeout(),
                Is.EqualTo(QueenZoneSqlServerOptions.LongRunningCommandTimeoutSeconds));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy", null);
        }
    }
}
