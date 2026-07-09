namespace QueenZone.Web;

public static class PublicOutputCachePolicies
{
    public const string PublicSitemaps = "public-sitemaps";

    /// <summary>
    /// Output-cache tag applied to robots/sitemap routes. Evict with
    /// <see cref="Microsoft.AspNetCore.OutputCaching.IOutputCacheStore.EvictByTagAsync"/> after
    /// public URL sets change (for example admin news publish).
    /// </summary>
    public const string PublicSitemapTag = "public-sitemap";

    public static readonly TimeSpan SitemapDuration = TimeSpan.FromHours(24);

    private static readonly string[] ExcludedPathPrefixes =
    [
        "/account",
        "/admin",
        "/health"
    ];

    public static bool IsPublicReadOnlyRequest(HttpContext httpContext)
    {
        if (!HttpMethods.IsGet(httpContext.Request.Method) &&
            !HttpMethods.IsHead(httpContext.Request.Method))
        {
            return false;
        }

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        var path = httpContext.Request.Path;
        return !ExcludedPathPrefixes.Any(prefix =>
            path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
