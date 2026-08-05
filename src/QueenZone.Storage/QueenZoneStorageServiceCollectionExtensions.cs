using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QueenZone.Storage;

public static class QueenZoneStorageServiceCollectionExtensions
{
    /// <summary>
    /// Local-dev fallback when <c>ConnectionStrings:BlobStorage</c> is unset: uploads throw
    /// <see cref="NullBlobUploadService.NotConfiguredMessage"/> instead of silently succeeding
    /// against nothing, so a missing setting fails loud rather than corrupting data.
    /// </summary>
    public static IServiceCollection AddQueenZoneInMemoryStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BlobUploadOptions>()
            .Bind(configuration.GetSection(BlobUploadOptions.SectionName));
        services.AddSingleton<IBlobUploadService, NullBlobUploadService>();
        services.AddGalleryPhotoBlobService();
        return services;
    }

    /// <summary>
    /// Testing/E2E composition (<c>QueenZoneEnvironments.UsesInMemoryBlobStorage</c> in
    /// QueenZone.Web): unlike <see cref="AddQueenZoneInMemoryStorage"/>, uploads here must
    /// actually succeed — member photo/article/avatar submission flows run for real against the
    /// SQL Express mirror in the E2E environment — just without an Azure dependency or bytes
    /// that survive a process restart. <see cref="AzureBlobUploadService"/> is backend-agnostic
    /// despite its name — it only talks to <see cref="IBlobStorageBackend"/> — so it is reused
    /// here with <see cref="InMemoryBlobStorageBackend"/> instead of duplicating validation and
    /// content-sniffing logic in a second upload service.
    /// </summary>
    public static IServiceCollection AddQueenZoneFunctionalInMemoryStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BlobUploadOptions>()
            .Bind(configuration.GetSection(BlobUploadOptions.SectionName));
        services.AddSingleton<IBlobStorageBackend, InMemoryBlobStorageBackend>();
        services.AddSingleton<IBlobUploadService>(sp => new AzureBlobUploadService(
            sp.GetRequiredService<IBlobStorageBackend>(),
            sp.GetRequiredService<IOptions<BlobUploadOptions>>()));
        services.AddGalleryPhotoBlobService();
        return services;
    }

    public static IServiceCollection AddQueenZoneStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(BlobUploadOptions.ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services.AddQueenZoneInMemoryStorage(configuration);
        }

        services.AddOptions<BlobUploadOptions>()
            .Bind(configuration.GetSection(BlobUploadOptions.SectionName));
        services.AddSingleton(_ => new BlobServiceClient(connectionString));
        services.AddSingleton<IBlobStorageBackend>(sp =>
            new AzureBlobStorageBackend(sp.GetRequiredService<BlobServiceClient>()));
        services.AddSingleton<IBlobUploadService, AzureBlobUploadService>();
        services.AddGalleryPhotoBlobService();
        return services;
    }
}
