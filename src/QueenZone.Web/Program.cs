using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Identity.Web;
using QueenZone.Data;
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

builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection(AdminOptions.SectionName));
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));
builder.Services.Configure<SitemapOptions>(builder.Configuration.GetSection(SitemapOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CoreSitemapBuilder>();
builder.Services.AddSingleton<CoreSitemapService>();
builder.Services.AddAntiforgery();

var legacyConnectionString = builder.Configuration.GetConnectionString("QueenZoneLegacy");
var useLegacySql = !string.IsNullOrWhiteSpace(legacyConnectionString);

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "Admin");
});

if (useLegacySql)
{
    builder.Services.AddQueenZoneLegacyData(legacyConnectionString!);
}
else
{
    builder.Services.AddQueenZoneInMemoryData();
}

ConfigureAuthentication(builder);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => IsAdminEmail(context.User, builder.Configuration)));
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Minimal liveness probe used by CI smoke/e2e checks and any future uptime monitoring.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapSitemapEndpoints();
app.MapRazorPages();

app.Run();

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        return;
    }

    var azureAdSection = builder.Configuration.GetSection("AzureAd");
    var clientId = azureAdSection["ClientId"];

    if (string.IsNullOrWhiteSpace(clientId))
    {
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        return;
    }

    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration);
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
