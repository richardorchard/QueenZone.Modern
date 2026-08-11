using System.Security.Cryptography;
using System.Text;

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
    /// Length of the hashed <see cref="RunnerSuffixLength"/> runner suffix (see
    /// <see cref="ResolveRunId"/>), including its leading hyphen.
    /// </summary>
    public const int RunnerSuffixLength = 7;

    /// <summary>
    /// <c>GITHUB_RUN_ID</c> in CI, otherwise a UTC <c>yyyyMMddHHmmss</c> timestamp. When
    /// <c>RUNNER_NAME</c> is set, a short deterministic hash of it is appended so parallel
    /// matrix legs that share a <c>GITHUB_RUN_ID</c> (e.g. the Windows and macOS RealData
    /// jobs, which both write to the same shared SQL Express mirror) never allocate
    /// colliding markers. A hash (rather than the raw runner name) keeps the suffix short
    /// and bounded, since fixture titles built from markers are length-constrained.
    /// </summary>
    public static string ResolveRunId(Func<string, string?>? getEnv = null)
    {
        getEnv ??= static name => Environment.GetEnvironmentVariable(name);

        string baseId;
        var githubRunId = getEnv("GITHUB_RUN_ID");
        if (!string.IsNullOrWhiteSpace(githubRunId))
        {
            baseId = githubRunId.Trim();
        }
        else
        {
            baseId = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }

        var runnerSuffix = HashRunnerSuffix(getEnv("RUNNER_NAME"));
        return runnerSuffix is null ? baseId : $"{baseId}-{runnerSuffix}";
    }

    private static string? HashRunnerSuffix(string? runnerName)
    {
        if (string.IsNullOrWhiteSpace(runnerName))
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(runnerName.Trim()));
        return Convert.ToHexString(hash)[..6].ToLowerInvariant();
    }

    public static bool IsReadOnlyMode(Func<string, string?>? getEnv = null)
    {
        getEnv ??= static name => Environment.GetEnvironmentVariable(name);
        var value = getEnv("E2E_READONLY");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
    }
}
