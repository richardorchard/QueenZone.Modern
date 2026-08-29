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
        var service = new SampleGalleryImageSeedHostedService(
            gallery,
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
}
