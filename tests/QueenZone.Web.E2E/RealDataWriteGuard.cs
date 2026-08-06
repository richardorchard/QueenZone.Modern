namespace QueenZone.Web.E2E;

/// <summary>
/// Structural write protection for RealData fixtures when the live-site job sets
/// <c>E2E_READONLY=true</c>. Invoked from <see cref="RealDataPageTest"/> setup so a
/// write-capable fixture cannot run against production even if a filter is misconfigured.
/// </summary>
internal static class RealDataWriteGuard
{
    /// <summary>
    /// Throws when a write-capable fixture is about to run under read-only mode.
    /// </summary>
    /// <param name="allowsWrites">Whether the fixture may write to the database.</param>
    /// <param name="fixtureName">Fixture type name for the error message.</param>
    /// <param name="getEnv">Optional env lookup (tests inject; production uses process env).</param>
    public static void EnsureNotWriteCapableInReadOnlyMode(
        bool allowsWrites,
        string fixtureName,
        Func<string, string?>? getEnv = null)
    {
        if (!allowsWrites)
        {
            return;
        }

        if (!RealDataMarkers.IsReadOnlyMode(getEnv))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{fixtureName} allows writes but E2E_READONLY=true. " +
            "Write-capable RealData fixtures must not run in the live-site read-only job. " +
            "Use --filter TestCategory=ReadOnly (or a read-only fixture base) instead.");
    }
}
