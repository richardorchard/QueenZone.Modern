namespace QueenZone.Web.E2E;

/// <summary>
/// Base for nightly real-SQL Playwright fixtures (<see cref="E2ECategories.RealData"/>).
/// Shares tracing and failure artifact capture from <see cref="E2EPageTest"/>.
/// <para>
/// <b>Marker convention</b> (matches probe discipline such as <c>photo-submission-probe-%</c>):
/// <c>uie2e-{runId}-{fixture}-{n}</c>
/// where <c>runId</c> is <c>GITHUB_RUN_ID</c> in CI or a UTC timestamp locally, optionally
/// suffixed with the sanitized <c>RUNNER_NAME</c> so parallel matrix legs sharing one
/// <c>GITHUB_RUN_ID</c> (and one shared SQL Express mirror) don't collide,
/// <c>fixture</c> is a short kebab slug for the fixture, and <c>n</c> is a per-fixture counter.
/// Use <see cref="NextMarker"/> for any title/body/email the fixture inserts so residue checks
/// and <see cref="CleanupCreatedRowsAsync"/> can find the rows.
/// </para>
/// </summary>
public abstract class RealDataPageTest : E2EPageTest
{
    private int _markerSequence;
    private readonly List<string> _markers = [];

    /// <summary>
    /// When true (default), the fixture may write to the database. The live-site job sets
    /// <c>E2E_READONLY=true</c>; write-capable fixtures must throw rather than run.
    /// Override to <c>false</c> for read-only RealData fixtures (public route sweeps).
    /// </summary>
    protected virtual bool AllowsWrites => true;

    /// <summary>
    /// Shared run id for this process (GitHub Actions run id, or local timestamp).
    /// </summary>
    protected string RunId { get; private set; } = string.Empty;

    /// <summary>
    /// Markers allocated via <see cref="NextMarker"/> during the current test.
    /// </summary>
    protected IReadOnlyList<string> Markers => _markers;

    [SetUp]
    public void RealDataSetUp()
    {
        // Structural guarantee: write-capable fixtures never start under E2E_READONLY=true
        // (live-site job). Covered by RealDataWriteGuardTests — do not rely on filter convention alone.
        RealDataWriteGuard.EnsureNotWriteCapableInReadOnlyMode(AllowsWrites, GetType().Name);

        RunId = RealDataMarkers.ResolveRunId();
        _markerSequence = 0;
        _markers.Clear();
    }

    [TearDown]
    public async Task RealDataCleanupAsync()
    {
        if (_markers.Count == 0 || !AllowsWrites)
        {
            return;
        }

        try
        {
            await CleanupCreatedRowsAsync(_markers);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"RealData cleanup failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Allocates the next marker token for inserts owned by this fixture.
    /// </summary>
    /// <param name="fixtureSlug">Short kebab-case fixture id (for example <c>photo-submit</c>).</param>
    protected string NextMarker(string fixtureSlug)
    {
        var n = ++_markerSequence;
        var marker = RealDataMarkers.Format(RunId, fixtureSlug, n);
        _markers.Add(marker);
        return marker;
    }

    /// <summary>
    /// Delete rows created with the given markers. Default is a no-op until write fixtures
    /// wire repository/SQL cleanup (see nightly residue markers in Web.Tests).
    /// </summary>
    protected virtual Task CleanupCreatedRowsAsync(IReadOnlyList<string> markers) =>
        Task.CompletedTask;
}

