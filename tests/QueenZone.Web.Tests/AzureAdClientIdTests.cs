using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QueenZone.Web.Tests;

public sealed class AzureAdClientIdTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("YOUR_CLIENT_ID", false)]
    [InlineData("your_client_id", false)]
    [InlineData("CHANGE_ME", false)]
    [InlineData("TODO", false)]
    [InlineData("11111111-2222-3333-4444-555555555555", true)]
    [InlineData("real-app-id", true)]
    public void IsConfigured_matches_expected(string? clientId, bool expected)
    {
        Assert.Equal(expected, AzureAdClientId.IsConfigured(clientId));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("E2E")]
    public void EnsureConfiguredForEnvironment_allows_empty_client_in_dev_and_testing(string environmentName)
    {
        var env = new FakeHostEnvironment(environmentName);
        var exception = Record.Exception(() =>
            AzureAdClientId.EnsureConfiguredForEnvironment(env, clientId: null));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void EnsureConfiguredForEnvironment_throws_when_missing_outside_dev(string environmentName)
    {
        var env = new FakeHostEnvironment(environmentName);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AzureAdClientId.EnsureConfiguredForEnvironment(env, clientId: null));
        Assert.Contains("AzureAd:ClientId", ex.Message, StringComparison.Ordinal);
        Assert.Contains("X-Test-User-Email", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Production", "YOUR_CLIENT_ID")]
    [InlineData("Staging", "")]
    public void EnsureConfiguredForEnvironment_throws_on_placeholder_outside_dev(
        string environmentName,
        string clientId)
    {
        var env = new FakeHostEnvironment(environmentName);
        Assert.Throws<InvalidOperationException>(() =>
            AzureAdClientId.EnsureConfiguredForEnvironment(env, clientId));
    }

    [Fact]
    public void EnsureConfiguredForEnvironment_accepts_real_client_id_in_production()
    {
        var env = new FakeHostEnvironment("Production");
        var exception = Record.Exception(() =>
            AzureAdClientId.EnsureConfiguredForEnvironment(
                env,
                "11111111-2222-3333-4444-555555555555"));
        Assert.Null(exception);
    }

    [Fact]
    public void AddQueenZoneAuth_throws_in_production_without_client_id()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        var env = new FakeHostEnvironment("Production");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddQueenZoneAuth(configuration, env));

        Assert.Contains("AzureAd:ClientId", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddQueenZoneAuth_registers_test_scheme_in_development_without_client_id()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var env = new FakeHostEnvironment("Development");

        services.AddQueenZoneAuth(configuration, env);

        await using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(TestAuthHandler.SchemeName);
        Assert.NotNull(scheme);
        Assert.Equal(typeof(TestAuthHandler), scheme!.HandlerType);
    }

    [Fact]
    public async Task AddQueenZoneAuth_registers_test_member_scheme_in_e2e()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var env = new FakeHostEnvironment("E2E");

        services.AddQueenZoneAuth(configuration, env);
        services.AddQueenZoneAuthorization(configuration, env);

        await using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();

        var testAuthScheme = await schemeProvider.GetSchemeAsync(TestAuthHandler.SchemeName);
        Assert.NotNull(testAuthScheme);
        Assert.Equal(typeof(TestAuthHandler), testAuthScheme!.HandlerType);

        var testMemberScheme = await schemeProvider.GetSchemeAsync(TestMemberAuthHandler.SchemeName);
        Assert.NotNull(testMemberScheme);
        Assert.Equal(typeof(TestMemberAuthHandler), testMemberScheme!.HandlerType);

        var authorizationOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>().Value;
        var memberPolicy = authorizationOptions.GetPolicy(MemberAuthenticationSchemes.MemberPolicy);
        Assert.NotNull(memberPolicy);
        Assert.Contains(TestMemberAuthHandler.SchemeName, memberPolicy!.AuthenticationSchemes);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task AddQueenZoneAuth_does_not_register_test_scheme_in_production_with_real_client_id(string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = "11111111-2222-3333-4444-555555555555",
                ["AzureAd:TenantId"] = "common",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var env = new FakeHostEnvironment(environmentName);

        services.AddQueenZoneAuth(configuration, env);

        await using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(TestAuthHandler.SchemeName);
        Assert.Null(scheme);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
