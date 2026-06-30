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
builder.Services.Configure<MemberAuthenticationOptions>(builder.Configuration.GetSection(MemberAuthenticationOptions.SectionName));
builder.Services.Configure<ForumDataOptions>(builder.Configuration.GetSection(ForumDataOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CoreSitemapBuilder>();
builder.Services.AddSingleton<CoreSitemapService>();
builder.Services.AddSingleton<ForumSitemapBuilder>();
builder.Services.AddSingleton<SitemapIndexBuilder>();
builder.Services.AddScoped<MemberAccountService>();
builder.Services.AddAntiforgery();

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

ConfigureAuthentication(builder);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireAuthenticatedUser()
            .RequireAssertion(context => IsAdminEmail(context.User, builder.Configuration)));

    options.AddPolicy(MemberAuthenticationSchemes.MemberPolicy, policy =>
        policy.AddAuthenticationSchemes(MemberAuthenticationSchemes.MembersCookie)
            .RequireAuthenticatedUser());
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Minimal liveness probe used by CI smoke/e2e checks and any future uptime monitoring.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Proves the Member policy gate works end-to-end. No real content uses it yet — future
// write-features (forum posting, attachment downloads, fan-performance submissions) will
// apply [Authorize(Policy = "Member")] or this same RequireAuthorization call once built.
app.MapGet("/account/member-probe", () => Results.Ok(new { authenticated = true }))
    .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy);

app.MapSitemapEndpoints();
app.MapRazorPages();

app.Run();

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
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

    var azureAdSection = builder.Configuration.GetSection("AzureAd");
    var clientId = azureAdSection["ClientId"];

    if (string.IsNullOrWhiteSpace(clientId))
    {
        builder.Services
            .AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
    }
    else
    {
        builder.Services
            .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(builder.Configuration);
    }

    // A second AddAuthentication() call doesn't reset the default scheme set above; it just
    // returns a plain AuthenticationBuilder bound to the same AuthenticationOptions so the
    // member schemes can be chained on without fighting Microsoft.Identity.Web's own builder type.
    ConfigureMemberAuthentication(builder, builder.Services.AddAuthentication());
}

static void ConfigureMemberAuthentication(WebApplicationBuilder builder, AuthenticationBuilder authenticationBuilder)
{
    authenticationBuilder.AddCookie(MemberAuthenticationSchemes.MembersCookie, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

    authenticationBuilder.AddCookie(MemberAuthenticationSchemes.ExternalCookie, options =>
    {
        options.Cookie.Name = ".QueenZone.MembersExternal";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
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

    if (!string.IsNullOrWhiteSpace(memberAuth?.Facebook?.ClientId))
    {
        authenticationBuilder.AddFacebook(MemberAuthenticationSchemes.Facebook, options =>
        {
            options.AppId = memberAuth.Facebook.ClientId!;
            options.AppSecret = memberAuth.Facebook.ClientSecret!;
            options.SignInScheme = MemberAuthenticationSchemes.ExternalCookie;
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
