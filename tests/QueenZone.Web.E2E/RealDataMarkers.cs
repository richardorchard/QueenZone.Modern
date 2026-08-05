namespace QueenZone.Web.E2E;

/// <summary>
/// Marker tokens for RealData UI fixtures: <c>uie2e-{runId}-{fixture}-{n}</c>.
/// Same discipline as probe scripts (<c>photo-submission-probe-%</c>).
/// </summary>
internal static class RealDataMarkers
{
    public const string Prefix = "uie2e-";

    /// <summary>
    /// Formats a marker. <paramref name="fixtureSlug"/> must be non-empty kebab-case.
    /// </summary>
    public static string Format(string runId, string fixtureSlug, int sequence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureSlug);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);

        var slug = fixtureSlug.Trim().ToLowerInvariant();
        return $"{Prefix}{runId.Trim()}-{slug}-{sequence}";
    }

    /// <summary>
    /// <c>GITHUB_RUN_ID</c> in CI, otherwise a UTC <c>yyyyMMddHHmmss</c> timestamp.
    /// </summary>
    public static string ResolveRunId(Func<string, string?>? getEnv = null)
    {
        getEnv ??= static name => Environment.GetEnvironmentVariable(name);
        var githubRunId = getEnv("GITHUB_RUN_ID");
        if (!string.IsNullOrWhiteSpace(githubRunId))
        {
            return githubRunId.Trim();
        }

        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }

    public static bool IsReadOnlyMode(Func<string, string?>? getEnv = null)
    {
        getEnv ??= static name => Environment.GetEnvironmentVariable(name);
        var value = getEnv("E2E_READONLY");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }
}
