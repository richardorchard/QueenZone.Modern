namespace QueenZone.Web;

/// <summary>
/// Helpers for LCP-related image attributes used by <c>_PictureImage</c>.
/// The partial still takes the historical 4-tuple model for Razor compatibility.
/// </summary>
public static class PictureImageAttributes
{
    /// <summary>
    /// Eager design-system hero JPEGs (img-hero) are LCP candidates.
    /// </summary>
    public static string? ResolveFetchPriority(string src, bool lazy)
    {
        if (lazy)
        {
            return null;
        }

        if (src.Contains("img-hero", StringComparison.OrdinalIgnoreCase))
        {
            return "high";
        }

        return null;
    }
}
