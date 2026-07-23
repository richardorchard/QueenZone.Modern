using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using QueenZone.Web;
using QueenZone.Web.Health;
using QueenZone.Web.Sitemap;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Local secrets only in Development. Loading them for Production/Staging would let a
// developer machine's empty AzureAd:ClientId override App Service settings when
// ASPNETCORE_ENVIRONMENT is mis-set, and would break production-shaped integration tests.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    // Keep environment variables above Local.json so CI/shell overrides still win.
    builder.Configuration.AddEnvironmentVariables();
}
else if (builder.Environment.IsEnvironment("Testing"))
{
    // Avoid DPAPI-backed key persistence under the service profile, which is slow
    // (and unnecessary for short-lived smoke test runs) on the self-hosted CI runner.
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

builder.Services.AddQueenZoneApplicationInsights(
    builder.Configuration,
    builder.Environment,
    builder.Logging);

builder.Services.AddQueenZoneWebComposition(builder.Configuration, builder.Environment);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "Admin");
});

var app = builder.Build();

// Azure App Service (and any CDN/proxy in front of it, e.g. Cloudflare) terminates TLS and
// forwards plain HTTP internally. Without this, Request.Scheme/Host reflect the internal
// hop, so OAuth providers (Google/Microsoft/Facebook) get built redirect_uri values like
// http://<internal-host> instead of https://queenzone.org â€” which then fail to match the
// redirect URI registered with each provider. KnownIPNetworks/KnownProxies are cleared because
// the edge proxy IP isn't a fixed, known address; the app already trusts App Service/Cloudflare
// as its only ingress.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

if (ResponseCompressionBootstrap.IsEnabled(app.Environment))
{
    app.UseResponseCompression();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

// PhysicalFileProvider excludes dot-prefixed files/folders by default, so the generic
// UseStaticFiles() below would 404 on /.well-known/* (used for Microsoft's domain
// association file, and any future well-known files like ACME challenges). Map it
// explicitly with exclusion filtering turned off before the catch-all static handler.
// WebRootPath can be null if no physical wwwroot folder is found relative to whatever
// the process's working directory happens to be at startup (e.g. a misconfigured launch
// script) â€” guard rather than crash the whole app over an optional route.
var wellKnownPath = string.IsNullOrEmpty(app.Environment.WebRootPath)
    ? null
    : Path.Combine(app.Environment.WebRootPath, ".well-known");
if (wellKnownPath is not null && Directory.Exists(wellKnownPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(wellKnownPath, ExclusionFilters.None),
        RequestPath = "/.well-known",
    });
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    SecurityHeaders.Apply(context);
    await next();
});

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => StaticFileCacheControl.Apply(ctx.Context, app.Environment),
});
app.UseAuthentication();
// Public pages use a non-member default scheme; without this, HttpContext.User stays
// anonymous while the MembersCookie is present. Antiforgery tokens then fail on member-only
// APIs (e.g. editor image upload) because generation and validation see different identities.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        var member = await context.AuthenticateAsync(MemberAuthenticationSchemes.MembersCookie);
        if (member.Succeeded && member.Principal?.Identity?.IsAuthenticated == true)
        {
            context.User = member.Principal;
        }
    }

    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAntiforgery();

// Liveness (/health) + readiness (/health/ready) — see QueenZoneHealthEndpoints.
app.MapQueenZoneHealthEndpoints();

// Member-gated fan-performance audio downloads use FanPerformanceEndpoints.
app.MapGet("/account/member-probe", () => Results.Ok(new { authenticated = true }))
    .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy);

app.MapFanPerformanceEndpoints();
app.MapForumAttachmentEndpoints();
app.MapForumPollEndpoints();
app.MapEditorImageUploadEndpoints();
app.MapMemberAvatarEndpoints();
app.MapUgcProxyEndpoints();
app.MapSitemapEndpoints();
app.MapArticleAutosaveEndpoint();
app.MapArticlesFeedEndpoint();
// Anonymous public HTML is output-cached (short TTL); policy no-ops for authenticated users
// and for the Testing environment so integration suites stay deterministic.
app.MapRazorPages().CacheOutput(PublicOutputCachePolicies.PublicHtml);
app.MapFallbackToPage("/NotFound");

app.Run();

public partial class Program;

