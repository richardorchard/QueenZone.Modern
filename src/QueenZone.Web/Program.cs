using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.GitHub;
using System.Security.Claims;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Identity.Web;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using QueenZone.Data;
using QueenZone.NewsAgent;
using QueenZone.Web;
using QueenZone.Web.Sitemap;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}
else
{
    // Avoid DPAPI-backed key persistence under the service profile, which is slow
    // (and unnecessary for short-lived smoke test runs) on the self-hosted CI runner.
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

ConfigureApplicationInsights(builder);

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));
builder.Services.Configure<SitemapOptions>(builder.Configuration.GetSection(SitemapOptions.SectionName));
builder.Services.Configure<MemberAuthenticationOptions>(builder.Configuration.GetSection(MemberAuthenticationOptions.SectionName));
builder.Services.Configure<ForumDataOptions>(builder.Configuration.GetSection(ForumDataOptions.SectionName));
builder.Services.Configure<PublicQueryCacheOptions>(builder.Configuration.GetSection(PublicQueryCacheOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(PublicOutputCachePolicies.PublicSitemaps, policy => policy
        .With(context => PublicOutputCachePolicies.IsPublicReadOnlyRequest(context.HttpContext))
        .Expire(PublicOutputCachePolicies.SitemapDuration)
        .SetVaryByRouteValue("*")
        .Tag("public-sitemap"));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<CoreSitemapBuilder>();
builder.Services.AddSingleton<CoreSitemapService>();
builder.Services.AddSingleton<ForumSitemapBuilder>();
builder.Services.AddSingleton<SitemapIndexBuilder>();
builder.Services.AddScoped<PublicQueryCacheService>();
builder.Services.AddScoped<MemberAccountService>();
builder.Services.AddAntiforgery();

if (ResponseCompressionBootstrap.IsEnabled(builder.Environment))
{
    ResponseCompressionBootstrap.ConfigureServices(builder.Services);
}

var legacyConnectionString = builder.Configuration.GetConnectionString("QueenZoneLegacy");
var useLegacySql = !string.IsNullOrWhiteSpace(legacyConnectionString);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "Admin");
});

if (useLegacySql)
{
    var forumDataOptions = builder.Configuration
        .GetSection(ForumDataOptions.SectionName)
        .Get<ForumDataOptions>() ?? new ForumDataOptions();

    builder.Services.AddQueenZoneLegacyData(legacyConnectionString!, forumDataOptions);
}
else
{
    builder.Services.AddQueenZoneInMemoryData();
}

builder.Services.AddQueenZoneNewsAgent(builder.Configuration);

ConfigureAuthentication(builder);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(context => IsAdminEmail(context.User, builder.Configuration)));

    options.AddPolicy(MemberAuthenticationSchemes.MemberPolicy, policy =>
        policy.AddAuthenticationSchemes(MemberAuthenticationSchemes.MembersCookie)
            .RequireAuthenticatedUser());
});

var app = builder.Build();

// Azure App Service (and any CDN/proxy in front of it, e.g. Cloudflare) terminates TLS and
// forwards plain HTTP internally. Without this, Request.Scheme/Host reflect the internal
// hop, so OAuth providers (Google/Microsoft/Facebook) get built redirect_uri values like
// http://<internal-host> instead of https://queenzone.org — which then fail to match the
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

// PhysicalFileProvider excludes dot-prefixed files/folders by default, so the generic
// UseStaticFiles() below would 404 on /.well-known/* (used for Microsoft's domain
// association file, and any future well-known files like ACME challenges). Map it
// explicitly with exclusion filtering turned off before the catch-all static handler.
// WebRootPath can be null if no physical wwwroot folder is found relative to whatever
// the process's working directory happens to be at startup (e.g. a misconfigured launch
// script) — guard rather than crash the whole app over an optional route.
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
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStatusCodePages(async statusCodeContext =>
{
    if (statusCodeContext.HttpContext.Response.StatusCode == 404)
    {
        var originalPath = statusCodeContext.HttpContext.Request.Path;
        statusCodeContext.HttpContext.Request.Path = "/404";
        await statusCodeContext.Next(statusCodeContext.HttpContext);
        statusCodeContext.HttpContext.Request.Path = originalPath;
    }
});
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => StaticFileCacheControl.Apply(ctx.Context, app.Environment),
});
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();
app.UseAntiforgery();

// Minimal liveness probe used by CI smoke/e2e checks and any future uptime monitoring.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Member-gated fan-performance audio downloads use FanPerformanceEndpoints.
app.MapGet("/account/member-probe", () => Results.Ok(new { authenticated = true }))
    .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy);

app.MapFanPerformanceEndpoints();
app.MapSitemapEndpoints();
app.MapRazorPages();

app.Run();

static void ConfigureApplicationInsights(WebApplicationBuilder builder)
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        return;
    }

    var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    var section = builder.Configuration.GetSection("ApplicationInsights");
    var tracesPerSecond = section.GetValue<double?>("TracesPerSecond") ?? 0.2;
    var enableLiveMetrics = section.GetValue<bool?>("EnableLiveMetrics") ?? false;
    var enableTraceBasedLogsSampler = section.GetValue<bool?>("EnableTraceBasedLogsSampler") ?? true;
    var exportedLogLevel = section.GetValue<LogLevel?>("ExportedLogLevel") ?? LogLevel.Warning;

    builder.Logging.AddFilter<OpenTelemetryLoggerProvider>(null, exportedLogLevel);

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: "QueenZone.Web",
            serviceNamespace: "QueenZone"))
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = connectionString;
            options.TracesPerSecond = tracesPerSecond;
            options.EnableLiveMetrics = enableLiveMetrics;
            options.EnableTraceBasedLogsSampler = enableTraceBasedLogsSampler;
        });
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var azureAdSection = builder.Configuration.GetSection("AzureAd");
    var clientId = azureAdSection["ClientId"];
    var useAzureAd = !builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(clientId);

    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                ConfigureAdminAuthenticationScheme(options, useAzureAd: false))
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null)
            // A real (not test-shortcut) cookie scheme: native register/sign-in pages call
            // SignInAsync, which requires a handler that actually implements sign-in, and a
            // local cookie has no external dependency that would make it unsuitable for tests.
            .AddCookie(MemberAuthenticationSchemes.MembersCookie, options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
            });
        return;
    }

    if (string.IsNullOrWhiteSpace(clientId))
    {
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                ConfigureAdminAuthenticationScheme(options, useAzureAd: false))
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
    }
    else
    {
        builder.Services
            .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration);

        builder.Services.AddAuthentication()
            .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                ConfigureAdminAuthenticationScheme(options, useAzureAd: true));
    }

    // A second AddAuthentication() call doesn't reset the default scheme set above; it just
    // returns a plain AuthenticationBuilder bound to the same AuthenticationOptions so the
    // member schemes can be chained on without fighting Microsoft.Identity.Web's own builder type.
    ConfigureMemberAuthentication(builder, builder.Services.AddAuthentication());
}

static void ConfigureAdminAuthenticationScheme(PolicySchemeOptions options, bool useAzureAd)
{
    options.ForwardDefaultSelector = context =>
        context.Request.Cookies.ContainsKey(AdminAuthenticationSchemes.MemberCookieName)
            ? MemberAuthenticationSchemes.MembersCookie
            : useAzureAd
                ? CookieAuthenticationDefaults.AuthenticationScheme
                : TestAuthHandler.SchemeName;

    options.ForwardChallenge = useAzureAd
        ? MemberAuthenticationSchemes.MembersCookie
        : TestAuthHandler.SchemeName;
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
static void ConfigureMemberAuthentication(WebApplicationBuilder builder, AuthenticationBuilder authenticationBuilder)
{
    authenticationBuilder.AddCookie(MemberAuthenticationSchemes.MembersCookie, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Lax (not Strict): the OAuth callback is a top-level GET redirected back from the
        // external provider's domain, and Strict would drop the cookie on that navigation.
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    authenticationBuilder.AddCookie(MemberAuthenticationSchemes.ExternalCookie, options =>
    {
        options.Cookie.Name = ".QueenZone.MembersExternal";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    var memberAuth = builder.Configuration.GetSection(MemberAuthenticationOptions.SectionName).Get<MemberAuthenticationOptions>();

    if (!string.IsNullOrWhiteSpace(memberAuth?.Google?.ClientId))
    {
        authenticationBuilder.AddGoogle(MemberAuthenticationSchemes.Google, options =>
        {
            options.ClientId = memberAuth.Google.ClientId!;
            options.ClientSecret = memberAuth.Google.ClientSecret!;
            options.SignInScheme = MemberAuthenticationSchemes.ExternalCookie;
        });
    }

    if (!string.IsNullOrWhiteSpace(memberAuth?.Microsoft?.ClientId))
    {
        authenticationBuilder.AddMicrosoftAccount(MemberAuthenticationSchemes.Microsoft, options =>
        {
            options.ClientId = memberAuth.Microsoft.ClientId!;
            options.ClientSecret = memberAuth.Microsoft.ClientSecret!;
            options.SignInScheme = MemberAuthenticationSchemes.ExternalCookie;
        });
    }

    if (!string.IsNullOrWhiteSpace(memberAuth?.Discord?.ClientId))
    {
        authenticationBuilder.AddDiscord(MemberAuthenticationSchemes.Discord, options =>
        {
            options.ClientId = memberAuth.Discord.ClientId!;
            options.ClientSecret = memberAuth.Discord.ClientSecret!;
            options.SignInScheme = MemberAuthenticationSchemes.ExternalCookie;
            options.Scope.Add("email");
        });
    }

    if (!string.IsNullOrWhiteSpace(memberAuth?.GitHub?.ClientId))
    {
        authenticationBuilder.AddGitHub(MemberAuthenticationSchemes.GitHub, options =>
        {
            options.ClientId = memberAuth.GitHub.ClientId!;
            options.ClientSecret = memberAuth.GitHub.ClientSecret!;
            options.SignInScheme = MemberAuthenticationSchemes.ExternalCookie;
            options.Scope.Add("user:email");
        });
    }
}

static bool IsAdminEmail(ClaimsPrincipal user, IConfiguration configuration)
{
    var email = user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue("preferred_username")
        ?? user.Identity?.Name;

    if (string.IsNullOrWhiteSpace(email))
    {
        return false;
    }

    var allowedEmails = configuration.GetSection(AdminOptions.SectionName).Get<AdminOptions>()?.AllowedEmails ?? [];
    return allowedEmails.Any(allowed =>
        string.Equals(allowed, email, StringComparison.OrdinalIgnoreCase));
}

public partial class Program;
