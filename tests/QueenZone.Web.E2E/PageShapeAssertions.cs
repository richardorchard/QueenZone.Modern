using System.Text.RegularExpressions;

namespace QueenZone.Web.E2E;

/// <summary>
/// Structural page checks (unrendered HTML-encoding artifacts, actionable browser console
/// errors) shared between the deterministic PR-gating smoke suite (<see cref="SmokeTests"/>)
/// and the nightly real-data sitemap sweep (<see cref="SitemapPublicRouteSweepTests"/>).
/// </summary>
internal static class PageShapeAssertions
{
    private static readonly Regex EncodingArtifactPattern = new(
        @"&(amp|lt|gt|quot|apos|\#39|nbsp);",
        RegexOptions.Compiled);

    public static Match FindEncodingArtifact(string bodyText) => EncodingArtifactPattern.Match(bodyText);

    // Mirror archives and CDN thumbs commonly 404 without indicating a real regression.
    public static bool IsActionableConsoleError(string message) =>
        !message.Contains("status of 404", StringComparison.OrdinalIgnoreCase);
}
