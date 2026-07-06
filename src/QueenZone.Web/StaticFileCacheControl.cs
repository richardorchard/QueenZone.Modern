namespace QueenZone.Web;

public static class StaticFileCacheControl
{
    public const string VersionedCacheHeader = "public, max-age=31536000, immutable";
    public const string UnversionedCacheHeader = "public, max-age=86400";

    public static void Apply(HttpContext context, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        context.Response.Headers.CacheControl = IsVersionedRequest(context.Request)
            ? VersionedCacheHeader
            : UnversionedCacheHeader;
    }

    public static bool IsVersionedRequest(HttpRequest request) =>
        request.Query.ContainsKey("v");
}
