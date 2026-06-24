namespace QueenZone.Web.Sitemap;

public static class SiteUrl
{
    public static string ToAbsolute(string publicBaseUrl, string path)
    {
        var baseUrl = publicBaseUrl.TrimEnd('/');
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{baseUrl}{normalizedPath}";
    }
}