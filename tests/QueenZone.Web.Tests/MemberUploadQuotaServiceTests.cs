using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class MemberUploadQuotaServiceTests
{
    [Fact]
    public void TryConsume_allows_until_count_limit()
    {
        var service = CreateService(maxUploads: 2, maxBytes: 1_000_000);

        Assert.True(service.TryConsume("member:a", 10, out _));
        Assert.True(service.TryConsume("member:a", 10, out _));
        Assert.False(service.TryConsume("member:a", 10, out var error));
        Assert.Contains("Daily upload limit", error);
    }

    [Fact]
    public void TryConsume_allows_until_byte_limit()
    {
        var service = CreateService(maxUploads: 100, maxBytes: 100);

        Assert.True(service.TryConsume("member:b", 60, out _));
        Assert.False(service.TryConsume("member:b", 50, out var error));
        Assert.Contains("size limit", error);
    }

    [Fact]
    public void TryConsume_is_isolated_per_principal()
    {
        var service = CreateService(maxUploads: 1, maxBytes: 1_000_000);

        Assert.True(service.TryConsume("member:one", 1, out _));
        Assert.True(service.TryConsume("member:two", 1, out _));
        Assert.False(service.TryConsume("member:one", 1, out _));
    }

    [Fact]
    public void TryConsume_batch_counts_multiple_uploads()
    {
        var service = CreateService(maxUploads: 3, maxBytes: 1_000_000);

        Assert.True(service.TryConsume("member:batch", 30, out _, uploadCount: 3));
        Assert.False(service.TryConsume("member:batch", 1, out _, uploadCount: 1));
    }

    [Fact]
    public void TryConsume_disabled_always_allows()
    {
        var service = CreateService(maxUploads: 0, maxBytes: 0, enabled: false);
        Assert.True(service.TryConsume("anyone", 999_999, out _));
    }

    [Fact]
    public void PrincipalKeyFromMemberId_formats_guid()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("member:11111111111111111111111111111111", MemberUploadQuotaService.PrincipalKeyFromMemberId(id));
        Assert.Equal("anonymous", MemberUploadQuotaService.PrincipalKeyFromMemberId(Guid.Empty));
    }

    private static MemberUploadQuotaService CreateService(
        int maxUploads,
        long maxBytes,
        bool enabled = true) =>
        new(
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            Options.Create(new UploadQuotaOptions
            {
                Enabled = enabled,
                MaxUploadsPerDay = maxUploads,
                MaxBytesPerDay = maxBytes,
            }));
}
