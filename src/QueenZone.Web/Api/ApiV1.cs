namespace QueenZone.Web;

/// <summary>
/// Versioned JSON API surface for the website and mobile app.
/// Separate from unversioned narrow endpoints under <c>/api/uploads</c>, RSS, and streaming.
/// </summary>
public static class ApiV1
{
    public const string Version = "v1";

    public const string Prefix = "/api/v1";

    public const string OpenApiDocumentName = "v1";

    /// <summary>Route pattern for <c>MapOpenApi</c>; must include <c>{documentName}</c>.</summary>
    public const string OpenApiRoutePattern = "/openapi/{documentName}.json";

    public const string OpenApiPath = "/openapi/v1.json";

    public static bool IsApiPath(PathString path) =>
        path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase);
}
