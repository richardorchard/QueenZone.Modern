namespace QueenZone.Web.E2E;

internal static class E2EArtifactPaths
{
    /// <summary>
    /// Directory for Playwright screenshots and traces on failure.
    /// Relative to the test process working directory (repo root in CI).
    /// </summary>
    public static string Root =>
        Environment.GetEnvironmentVariable("E2E_ARTIFACT_DIR")
        ?? Path.Combine("test-results", "e2e");

    public static string EnsureDirectory()
    {
        Directory.CreateDirectory(Root);
        return Root;
    }

    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Length <= 120 ? cleaned : cleaned[..120];
    }
}
