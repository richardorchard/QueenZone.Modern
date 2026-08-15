using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QueenZone.Web.Tests;

public sealed class DataProtectionBootstrapTests : IDisposable
{
    private readonly string testRoot = Path.Combine(
        Path.GetTempPath(),
        "QueenZone.Web.Tests",
        nameof(DataProtectionBootstrapTests),
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Production_keys_survive_a_new_service_provider()
    {
        var keysPath = Path.Combine(testRoot, "keys");
        var firstProvider = CreateProvider("Production", keysPath);
        var protectedValue = firstProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("restart-test")
            .Protect("cookie-value");
        firstProvider.Dispose();

        var secondProvider = CreateProvider("Production", keysPath);
        var unprotectedValue = secondProvider
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("restart-test")
            .Unprotect(protectedValue);
        secondProvider.Dispose();

        Assert.Equal("cookie-value", unprotectedValue);
        Assert.NotEmpty(Directory.EnumerateFiles(keysPath, "*.xml"));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Preview")]
    public void Production_like_hosts_reject_keys_under_wwwroot(string environmentName)
    {
        var webRoot = Path.Combine(testRoot, "wwwroot");
        Directory.CreateDirectory(webRoot);
        var environment = new TestWebHostEnvironment(environmentName, webRoot);
        var configuration = CreateConfiguration(Path.Combine(webRoot, "keys"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DataProtectionBootstrap.ConfigureServices(
                new ServiceCollection(),
                configuration,
                environment));

        Assert.Contains("outside the read-only wwwroot", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_rejects_relative_keys_path()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DataProtectionBootstrap.ConfigureServices(
                new ServiceCollection(),
                CreateConfiguration("data-protection-keys"),
                new TestWebHostEnvironment("Production", Path.Combine(testRoot, "wwwroot"))));

        Assert.Contains("must be an absolute path", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Testing_uses_ephemeral_keys_without_creating_configured_directory()
    {
        var keysPath = Path.Combine(testRoot, "keys");

        using var provider = CreateProvider("Testing", keysPath);
        _ = provider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("test")
            .Protect("value");

        Assert.False(Directory.Exists(keysPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(testRoot))
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    private ServiceProvider CreateProvider(string environmentName, string keysPath)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        DataProtectionBootstrap.ConfigureServices(
            services,
            CreateConfiguration(keysPath),
            new TestWebHostEnvironment(environmentName, Path.Combine(testRoot, "wwwroot")));
        return services.BuildServiceProvider();
    }

    private static IConfiguration CreateConfiguration(string keysPath) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DataProtectionBootstrap.KeysPathConfigurationKey] = keysPath,
            })
            .Build();

    private sealed class TestWebHostEnvironment(string environmentName, string webRootPath) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = webRootPath;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
