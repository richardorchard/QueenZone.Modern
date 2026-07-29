using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QueenZone.Web;

public static class VersionedStaticPath
{
    public static string Apply(IFileVersionProvider fileVersionProvider, PathString requestPathBase, string path) =>
        fileVersionProvider.AddFileVersionToPath(requestPathBase, path);

    public static string? TryGetVersionedWebpSrc(
        IFileVersionProvider fileVersionProvider,
        IWebHostEnvironment environment,
        PathString requestPathBase,
        string src)
    {
        if (!src.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            && !src.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var webpPath = string.Concat(src.AsSpan(0, src.Length - 4), ".webp");
        if (!WebpVariantExists(environment, webpPath))
        {
            return null;
        }

        return Apply(fileVersionProvider, requestPathBase, webpPath);
    }

    public static bool WebpVariantExists(IWebHostEnvironment environment, string webpPath)
    {
        if (string.IsNullOrEmpty(environment.WebRootPath))
        {
            return webpPath.Contains("/img-", StringComparison.OrdinalIgnoreCase);
        }

        var relative = webpPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return File.Exists(Path.Combine(environment.WebRootPath, relative));
    }
}
