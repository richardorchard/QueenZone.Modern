using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

namespace QueenZone.Web;

public static class QueenZoneAuthServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var azureAdSection = configuration.GetSection("AzureAd");
        var clientId = azureAdSection["ClientId"];
        var useAzureAd = !environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(clientId);

        if (environment.IsEnvironment("Testing"))
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
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
            return services;
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                    ConfigureAdminAuthenticationScheme(options, useAzureAd: false))
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        }
        else
        {
            services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(configuration);

            services.AddAuthentication()
                .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                    ConfigureAdminAuthenticationScheme(options, useAzureAd: true));
        }

        // A second AddAuthentication() call doesn't reset the default scheme set above; it just
        // returns a plain AuthenticationBuilder bound to the same AuthenticationOptions so the
        // member schemes can be chained on without fighting Microsoft.Identity.Web's own builder type.
        ConfigureMemberAuthentication(configuration, services.AddAuthentication());
        return services;
    }

    public static IServiceCollection AddQueenZoneAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => IsAdminEmail(context.User, configuration)));

            options.AddPolicy(MemberAuthenticationSchemes.MemberPolicy, policy =>
                policy.AddAuthenticationSchemes(MemberAuthenticationSchemes.MembersCookie)
                    .RequireAuthenticatedUser());

            // Shared authoring (rich text image upload). Composite scheme selects member cookie
            // when present, otherwise Entra/test admin auth (same as admin pages).
            options.AddPolicy("Authoring", policy =>
                policy.AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
                    .RequireAuthenticatedUser());
        });

        return services;
    }

    private static void ConfigureAdminAuthenticationScheme(PolicySchemeOptions options, bool useAzureAd)
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
    private static void ConfigureMemberAuthentication(
        IConfiguration configuration,
        AuthenticationBuilder authenticationBuilder)
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

        var memberAuth = configuration
            .GetSection(MemberAuthenticationOptions.SectionName)
            .Get<MemberAuthenticationOptions>();

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

    private static bool IsAdminEmail(ClaimsPrincipal user, IConfiguration configuration)
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
}
