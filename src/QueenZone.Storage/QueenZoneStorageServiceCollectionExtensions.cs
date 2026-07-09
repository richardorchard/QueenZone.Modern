using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Storage;

public static class QueenZoneStorageServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BlobUploadOptions>()
            .Bind(configuration.GetSection(BlobUploadOptions.SectionName));

        var connectionString = configuration.GetConnectionString(BlobUploadOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IBlobUploadService, NullBlobUploadService>();
            return services;
        }

        services.AddSingleton(_ => new BlobServiceClient(connectionString));
        services.AddSingleton<IBlobUploadService, AzureBlobUploadService>();
        return services;
    }
}
