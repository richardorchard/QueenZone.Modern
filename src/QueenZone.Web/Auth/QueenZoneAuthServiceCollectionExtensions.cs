using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using QueenZone.Data;

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
        // Fail closed outside Development/Testing when Entra is missing or still a placeholder.
        AzureAdClientId.EnsureConfiguredForEnvironment(environment, clientId);
        var useAzureAd = !QueenZoneEnvironments.UsesTestAuth(environment) && AzureAdClientId.IsConfigured(clientId);

        if (QueenZoneEnvironments.UsesTestAuth(environment))
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddPolicyScheme(AdminAuthenticationSchemes.CompositeScheme, null, options =>
                    ConfigureAdminAuthenticationScheme(options, useAzureAd: false))
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null)
                .AddScheme<AuthenticationSchemeOptions, TestMemberAuthHandler>(TestMemberAuthHandler.SchemeName, null)
                // A real (not test-shortcut) cookie scheme: native register/sign-in pages call
                // SignInAsync, which requires a handler that actually implements sign-in, and a
                // local cookie has no external dependency that would make it unsuitable for tests.
                .AddCookie(MemberAuthenticationSchemes.MembersCookie, options =>
                {
                    options.LoginPath = "/account/login";
                    options.LogoutPath = "/account/logout";
                    options.Events = MemberCookieEvents;
                });
            return services;
        }

        // Development-only fallback: empty/placeholder ClientId enables X-Test-User-Email admin auth.
        // Staging and Production must never reach this branch (guarded above).
        if (!useAzureAd)
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
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
                policy.AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
                    .RequireAuthenticatedUser()
                    .RequireAssertion(context => IsAdminEmail(context.User, configuration)));

            options.AddPolicy(MemberAuthenticationSchemes.MemberPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(MemberAuthenticationSchemes.MembersCookie);
                if (QueenZoneEnvironments.UsesTestAuth(environment))
                {
                    policy.AddAuthenticationSchemes(TestMemberAuthHandler.SchemeName);
                }

                policy.RequireAuthenticatedUser();
            });

            // Shared authoring (rich text image upload). Composite scheme selects member cookie
            // when present, otherwise Entra/test admin auth (same as admin pages). Members
            // impersonated purely via X-Test-Member-Id (no MembersCookie) still need to reach
            // this endpoint from Submit/Article and Submit/Photo's rich text editors, so add the
            // TestMember scheme the same way MemberPolicy above does.
            options.AddPolicy("Authoring", policy =>
            {
                policy.AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme);
                if (QueenZoneEnvironments.UsesTestAuth(environment))
                {
                    policy.AddAuthenticationSchemes(TestMemberAuthHandler.SchemeName);
                }

                policy.RequireAuthenticatedUser();
            });
        });

        return services;
    }

    private static void ConfigureAdminAuthenticationScheme(PolicySchemeOptions options, bool useAzureAd)
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Cookies.ContainsKey(AdminAuthenticationSchemes.MemberCookieName)
                ? MemberAuthenticationSchemes.MembersCookie
                : SelectAdminAuthenticateScheme(useAzureAd);

        // Challenges must start Entra OIDC (not member social login at /account/login).
        options.ForwardChallenge = SelectAdminChallengeScheme(useAzureAd);
    }

    /// <summary>
    /// Scheme used to authenticate an already-signed-in admin principal when no member cookie is present.
    /// </summary>
    internal static string SelectAdminAuthenticateScheme(bool useAzureAd) =>
        useAzureAd
            ? CookieAuthenticationDefaults.AuthenticationScheme
            : TestAuthHandler.SchemeName;

    /// <summary>
    /// Scheme used when the Admin policy challenges an unauthenticated request.
    /// With Entra enabled this must be OIDC, not the member cookie login path.
    /// </summary>
    internal static string SelectAdminChallengeScheme(bool useAzureAd) =>
        useAzureAd
            ? OpenIdConnectDefaults.AuthenticationScheme
            : TestAuthHandler.SchemeName;

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
            options.Events = MemberCookieEvents;
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

    /// <summary>
    /// Re-checks suspension status on every request that carries the members cookie, so a
    /// suspension takes effect immediately rather than waiting for the 30-day cookie to expire.
    /// Shared between the test-auth and Entra branches so both sign out a newly suspended member.
    /// </summary>
    private static CookieAuthenticationEvents MemberCookieEvents { get; } = new()
    {
        OnValidatePrincipal = async context =>
        {
            var memberIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (memberIdClaim is null || !Guid.TryParse(memberIdClaim, out var memberId))
            {
                return;
            }

            var repository = context.HttpContext.RequestServices.GetRequiredService<IMemberAccountRepository>();
            var account = await repository.FindByIdAsync(memberId, context.HttpContext.RequestAborted);
            if (account is null || account.IsSuspended)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(MemberAuthenticationSchemes.MembersCookie);
            }
        },
    };

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
