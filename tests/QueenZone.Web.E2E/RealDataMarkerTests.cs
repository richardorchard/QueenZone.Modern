namespace QueenZone.Web.E2E;

/// <summary>
/// Pure unit checks for RealData marker formatting and readonly guards (no browser).
/// </summary>
[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class RealDataMarkerTests
{
    [Test]
    public void Format_FollowsUie2eConvention()
    {
        var marker = RealDataMarkers.Format("987654321", "photo-submit", 2);
        Assert.That(marker, Is.EqualTo("uie2e-987654321-photo-submit-2"));
    }

    [Test]
    public void Format_NormalizesFixtureSlugToLower()
    {
        var marker = RealDataMarkers.Format("run1", "Photo-Submit", 1);
        Assert.That(marker, Is.EqualTo("uie2e-run1-photo-submit-1"));
    }

    [Test]
    public void ResolveRunId_PrefersGitHubRunId()
    {
        var runId = RealDataMarkers.ResolveRunId(name => name == "GITHUB_RUN_ID" ? " 42 " : null);
        Assert.That(runId, Is.EqualTo("42"));
    }

    [Test]
    public void ResolveRunId_FallsBackToTimestampShape()
    {
        var runId = RealDataMarkers.ResolveRunId(_ => null);
        Assert.That(runId, Does.Match(@"^\d{14}$"));
    }

    [Test]
    public void ResolveRunId_AppendsHashedRunnerSuffixWhenPresent()
    {
        var runId = RealDataMarkers.ResolveRunId(name => name switch
        {
            "GITHUB_RUN_ID" => "42",
            "RUNNER_NAME" => "Richards-Mac (queenzone)",
            _ => null,
        });
        Assert.That(runId, Does.Match(@"^42-[0-9a-f]{6}$"));
        Assert.That(runId.Length, Is.EqualTo(2 + RealDataMarkers.RunnerSuffixLength));
    }

    [Test]
    public void ResolveRunId_RunnerSuffixIsStableForSameRunnerName()
    {
        var first = RealDataMarkers.ResolveRunId(name => name switch
        {
            "GITHUB_RUN_ID" => "42",
            "RUNNER_NAME" => "richards-pc",
            _ => null,
        });
        var second = RealDataMarkers.ResolveRunId(name => name switch
        {
            "GITHUB_RUN_ID" => "99",
            "RUNNER_NAME" => "richards-pc",
            _ => null,
        });

        var firstSuffix = first[(first.IndexOf('-') + 1)..];
        var secondSuffix = second[(second.IndexOf('-') + 1)..];
        Assert.That(firstSuffix, Is.EqualTo(secondSuffix));
    }

    [Test]
    public void ResolveRunId_DifferentRunnerNamesProduceDifferentSuffixes()
    {
        var windows = RealDataMarkers.ResolveRunId(name => name switch
        {
            "GITHUB_RUN_ID" => "42",
            "RUNNER_NAME" => "richards-pc",
            _ => null,
        });
        var macOs = RealDataMarkers.ResolveRunId(name => name switch
        {
            "GITHUB_RUN_ID" => "42",
            "RUNNER_NAME" => "richards-mac-queenzone",
            _ => null,
        });
        Assert.That(windows, Is.Not.EqualTo(macOs));
    }

    [Test]
    public void ResolveRunId_OmitsRunnerSuffixWhenRunnerNameBlank()
    {
        var runId = RealDataMarkers.ResolveRunId(name => name == "GITHUB_RUN_ID" ? "42" : null);
        Assert.That(runId, Is.EqualTo("42"));
    }

    [Test]
    public void IsReadOnlyMode_TrueForTrueAndOne()
    {
        Assert.That(RealDataMarkers.IsReadOnlyMode(n => n == "E2E_READONLY" ? "true" : null), Is.True);
        Assert.That(RealDataMarkers.IsReadOnlyMode(n => n == "E2E_READONLY" ? "1" : null), Is.True);
        Assert.That(RealDataMarkers.IsReadOnlyMode(n => n == "E2E_READONLY" ? "false" : null), Is.False);
        Assert.That(RealDataMarkers.IsReadOnlyMode(_ => null), Is.False);
    }
}
