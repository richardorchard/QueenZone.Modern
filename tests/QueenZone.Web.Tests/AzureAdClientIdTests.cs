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

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
