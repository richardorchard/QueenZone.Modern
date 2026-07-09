using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class QueenZoneStorageRegistrationTests
{
    [Fact]
    public void Registers_null_service_when_connection_string_missing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddQueenZoneStorage(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<NullBlobUploadService>(provider.GetRequiredService<IBlobUploadService>());
    }

    [Fact]
    public void Registers_azure_service_when_connection_string_present()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Valid-shaped connection string; no network call in this test.
                ["ConnectionStrings:BlobStorage"] =
                    "DefaultEndpointsProtocol=https;AccountName=queenzonetest;AccountKey=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=;EndpointSuffix=core.windows.net",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddQueenZoneStorage(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<AzureBlobUploadService>(provider.GetRequiredService<IBlobUploadService>());
    }
}
