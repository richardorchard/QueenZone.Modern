using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;
using QueenZone.Storage;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class QueenZoneWebCompositionTests
{
    [Fact]
    public void AddQueenZoneWebComposition_registers_core_services_and_validates_options()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:AllowedEmails:0"] = "admin@test.local",
                ["Site:PublicBaseUrl"] = "https://www.queenzone.org",
                ["Sitemap:CacheHours"] = "24",
                ["PublicQueryCache:NewsCacheDuration"] = "00:05:00",
                ["PublicQueryCache:ArticleCountCacheDuration"] = "00:30:00",
                ["PublicQueryCache:ForumStatsCacheDuration"] = "00:30:00",
                ["PublicQueryCache:OnThisDayCacheDuration"] = "12:00:00",
                ["OpenRouter:DryRun"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddQueenZoneWebComposition(configuration, new FakeHostEnvironment("Testing"));

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.NotNull(provider.GetRequiredService<INewsRepository>());
        Assert.IsType<NullBlobUploadService>(provider.GetRequiredService<IBlobUploadService>());
        using (var scope = provider.CreateScope())
        {
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IBiographyRepository>());
            Assert.IsType<InMemoryBiographyRepository>(
                scope.ServiceProvider.GetRequiredService<IBiographyRepository>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<CoreSitemapService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<PublicQueryCacheService>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<MemberAccountService>());
        }

        Assert.Equal(["admin@test.local"], provider.GetRequiredService<IOptions<AdminOptions>>().Value.AllowedEmails);
        Assert.Equal("https://www.queenzone.org", provider.GetRequiredService<IOptions<SiteOptions>>().Value.PublicBaseUrl);
    }

    [Fact]
    public void AdminOptionsValidator_rejects_empty_allowed_emails()
    {
        var result = new AdminOptionsValidator().Validate(null, new AdminOptions { AllowedEmails = [] });
        Assert.True(result.Failed);
    }

    [Fact]
    public void SiteOptionsValidator_rejects_non_absolute_url()
    {
        var result = new SiteOptionsValidator().Validate(null, new SiteOptions { PublicBaseUrl = "not-a-url" });
        Assert.True(result.Failed);
    }

    [Fact]
    public void SitemapOptionsValidator_rejects_non_positive_cache_hours()
    {
        var result = new SitemapOptionsValidator().Validate(null, new SitemapOptions { CacheHours = 0 });
        Assert.True(result.Failed);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
