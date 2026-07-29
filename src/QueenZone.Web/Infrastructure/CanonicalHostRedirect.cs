using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public static class CanonicalHostRedirect
{
    public static IApplicationBuilder UseCanonicalHostRedirect(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var siteOptions = context.RequestServices.GetRequiredService<IOptions<SiteOptions>>().Value;
            if (TryBuildRedirectUrl(context.Request, siteOptions.PublicBaseUrl, out var redirectUrl))
            {
                context.Response.Redirect(redirectUrl, permanent: true);
                return;
            }

            await next();
        });

    internal static bool TryBuildRedirectUrl(HttpRequest request, string publicBaseUrl, out string redirectUrl)
    {
        redirectUrl = string.Empty;
        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var canonicalBase)
            || !canonicalBase.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var requestHost = request.Host.Host;
        var apexHost = canonicalBase.Host["www.".Length..];
        if (!string.Equals(requestHost, apexHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var canonicalUri = new UriBuilder(canonicalBase)
        {
            Path = request.PathBase.Add(request.Path).ToString(),
            Query = request.QueryString.HasValue ? request.QueryString.Value![1..] : string.Empty
        };

        redirectUrl = canonicalUri.Uri.AbsoluteUri;
        return true;
    }
}
