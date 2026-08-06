namespace QueenZone.Web.E2E;

/// <summary>
/// Proves the live-site write guard throws (issue #551), not merely that the env flag is parsed.
/// </summary>
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class RealDataWriteGuardTests
{
    [Test]
    public void WriteCapableFixture_ThrowsWhenE2EReadonlyIsTrue()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            RealDataWriteGuard.EnsureNotWriteCapableInReadOnlyMode(
                allowsWrites: true,
                fixtureName: "CommunitySubmissionWorkflowTests",
                getEnv: name => name == "E2E_READONLY" ? "true" : null));

        Assert.That(ex!.Message, Does.Contain("CommunitySubmissionWorkflowTests"));
        Assert.That(ex.Message, Does.Contain("E2E_READONLY=true"));
        Assert.That(ex.Message, Does.Contain("live-site"));
    }

    [Test]
    public void WriteCapableFixture_ThrowsWhenE2EReadonlyIsOne()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RealDataWriteGuard.EnsureNotWriteCapableInReadOnlyMode(
                allowsWrites: true,
                fixtureName: "WriteFixture",
                getEnv: name => name == "E2E_READONLY" ? "1" : null));
    }

    [Test]
    public void WriteCapableFixture_DoesNotThrowWhenReadonlyUnset()
    {
        Assert.DoesNotThrow(() =>
            RealDataWriteGuard.EnsureNotWriteCapableInReadOnlyMode(
                allowsWrites: true,
                fixtureName: "WriteFixture",
                getEnv: _ => null));
    }

    [Test]
    public void ReadOnlyFixture_DoesNotThrowWhenE2EReadonlyIsTrue()
    {
        Assert.DoesNotThrow(() =>
            RealDataWriteGuard.EnsureNotWriteCapableInReadOnlyMode(
                allowsWrites: false,
                fixtureName: "SitemapPublicRouteSweepTests",
                getEnv: name => name == "E2E_READONLY" ? "true" : null));
    }
}
