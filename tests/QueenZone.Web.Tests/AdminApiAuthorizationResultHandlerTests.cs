using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed class AdminApiAuthorizationResultHandlerTests
{
    [Fact]
    public void IsAdminApiPolicy_requires_admin_scheme_without_member_schemes()
    {
        var admin = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .Build();
        var memberApi = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(MemberAuthenticationSchemes.MembersBearer)
            .RequireAuthenticatedUser()
            .Build();
        var mixed = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(
                AdminAuthenticationSchemes.CompositeScheme,
                MemberAuthenticationSchemes.MembersBearer)
            .RequireAuthenticatedUser()
            .Build();

        var cookie = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(
                AdminAuthenticationSchemes.CompositeScheme,
                MemberAuthenticationSchemes.MembersCookie)
            .RequireAuthenticatedUser()
            .Build();

        Assert.True(AdminApiAuthorizationResultHandler.IsAdminApiPolicy(admin));
        Assert.False(AdminApiAuthorizationResultHandler.IsAdminApiPolicy(memberApi));
        Assert.False(AdminApiAuthorizationResultHandler.IsAdminApiPolicy(mixed));
        Assert.False(AdminApiAuthorizationResultHandler.IsAdminApiPolicy(cookie));
    }

    [Fact]
    public async Task HandleAsync_writes_problem_details_for_admin_api_challenges()
    {
        var handler = new AdminApiAuthorizationResultHandler();
        var http = CreateHttp("/api/v1/admin");
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            http,
            policy,
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
        Assert.Equal("application/problem+json", http.Response.ContentType?.Split(';')[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_writes_forbidden_problem_details_for_admin_api()
    {
        var handler = new AdminApiAuthorizationResultHandler();
        var http = CreateHttp("/api/v1/admin");
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .Build();

        await handler.HandleAsync(
            _ => Task.CompletedTask,
            http,
            policy,
            PolicyAuthorizationResult.Forbid());

        Assert.Equal(StatusCodes.Status403Forbidden, http.Response.StatusCode);
        Assert.Equal("application/problem+json", http.Response.ContentType?.Split(';')[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_invokes_next_when_authorization_succeeds()
    {
        var handler = new AdminApiAuthorizationResultHandler();
        var http = CreateHttp("/api/v1/admin");
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .Build();
        var invoked = false;

        await handler.HandleAsync(
            _ =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            http,
            policy,
            PolicyAuthorizationResult.Success());

        Assert.True(invoked);
        Assert.Equal(StatusCodes.Status200OK, http.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_leaves_member_api_challenges_to_the_default_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });
        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        http.Request.Path = "/api/v1/auth/session";
        http.Response.Body = new MemoryStream();
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(TestAuthHandler.SchemeName)
            .RequireAuthenticatedUser()
            .Build();

        await new AdminApiAuthorizationResultHandler().HandleAsync(
            _ => Task.CompletedTask,
            http,
            policy,
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
        Assert.NotEqual("application/problem+json", http.Response.ContentType?.Split(';')[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_leaves_website_admin_challenges_to_the_default_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication()
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                AdminAuthenticationSchemes.CompositeScheme, _ => { });
        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        http.Request.Path = "/admin";
        http.Response.Body = new MemoryStream();
        var policy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(AdminAuthenticationSchemes.CompositeScheme)
            .RequireAuthenticatedUser()
            .Build();

        await new AdminApiAuthorizationResultHandler().HandleAsync(
            _ => Task.CompletedTask,
            http,
            policy,
            PolicyAuthorizationResult.Challenge());

        Assert.Equal(StatusCodes.Status401Unauthorized, http.Response.StatusCode);
        Assert.NotEqual("application/problem+json", http.Response.ContentType?.Split(';')[0].Trim());
    }

    private static DefaultHttpContext CreateHttp(string path)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var http = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();
        return http;
    }
}
