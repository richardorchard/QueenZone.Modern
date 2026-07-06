namespace QueenZone.Web;

public static class PublicOutputCachePolicies
{
    public const string PublicArchivePages = "public-archive-pages";
    public const string PublicSitemaps = "public-sitemaps";

    public static readonly TimeSpan ArchivePageDuration = TimeSpan.FromMinutes(5);
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
