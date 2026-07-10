using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

/// <summary>
/// Opt-in round-trip against real Azure Storage.
/// Requires ConnectionStrings:BlobStorage and RUN_BLOB_STORAGE_TESTS=true.
/// </summary>
public sealed class AzureBlobUploadServiceIntegrationTests
{
    private static bool IsEnabled()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_BLOB_STORAGE_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__BlobStorage")
            ?? Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
        return !string.IsNullOrWhiteSpace(cs);
    }

    [Fact]
    public async Task Upload_and_delete_round_trip()
    {
        if (!IsEnabled())
        {
            return; // skipped without failing CI
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BlobStorage")
            ?? Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Blob storage connection string missing.");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BlobStorage"] = connectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddQueenZoneStorage(configuration);
        await using var provider = services.BuildServiceProvider();
        var upload = provider.GetRequiredService<IBlobUploadService>();

        // Minimal valid JPEG (SOI + APP0-ish + EOI)
        await using var stream = new MemoryStream(
        [
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0xFF, 0xD9
        ]);

        var result = await upload.UploadAsync(
            stream,
            "integration.jpg",
            BlobUploadContainers.Avatars,
            new BlobUploadContext { MemberId = 999_001 });

        Assert.Equal(BlobUploadContainers.Avatars, result.Container);
        Assert.StartsWith("members/999001/", result.BlobName);
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.True(result.SizeBytes > 0);

        var client = new BlobServiceClient(connectionString);
        var blob = client.GetBlobContainerClient(result.Container).GetBlobClient(result.BlobName);
        Assert.True(await blob.ExistsAsync());

        await upload.DeleteAsync(result.Container, result.BlobName);
        Assert.False((await blob.ExistsAsync()).Value);
    }
}
