using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class SampleGalleryImageSeedHostedServiceTests
{
    [Fact]
    public async Task StartAsync_seeds_missing_sample_originals_once()
    {
        var gallery = new NullGalleryPhotoBlobService();
        await using var provider = CreateProvider(gallery);
        var service = new SampleGalleryImageSeedHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SampleGalleryImageSeedHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await using (var first = await gallery.OpenReadAsync("brian-may", "img-101.jpg"))
        {
            Assert.NotNull(first);
            Assert.True(first.Length > 0);
        }

        await using (var zeroDims = await gallery.OpenReadAsync("brian-may", "img-103.jpg"))
        {
            Assert.NotNull(zeroDims);
        }

        await service.StartAsync(CancellationToken.None);
        await using var second = await gallery.OpenReadAsync("brian-may", "img-101.jpg");
        Assert.NotNull(second);
    }

    [Fact]
    public async Task StartAsync_resolves_scoped_gallery_blob_service_without_captive_dependency()
    {
        var gallery = new NullGalleryPhotoBlobService();
        var services = new ServiceCollection();
        services.AddScoped<IGalleryPhotoBlobService>(_ => gallery);
        services.AddSingleton<ILogger<SampleGalleryImageSeedHostedService>>(
            NullLogger<SampleGalleryImageSeedHostedService>.Instance);
        services.AddHostedService<SampleGalleryImageSeedHostedService>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        var hosted = Assert.Single(
            provider.GetServices<IHostedService>().OfType<SampleGalleryImageSeedHostedService>());
        await hosted.StartAsync(CancellationToken.None);

        await using var seeded = await gallery.OpenReadAsync("brian-may", "img-101.jpg");
        Assert.NotNull(seeded);
    }

    private static ServiceProvider CreateProvider(IGalleryPhotoBlobService gallery)
    {
        var services = new ServiceCollection();
        services.AddSingleton(gallery);
        return services.BuildServiceProvider();
    }
}
