using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace QueenZone.Web;

public static class ResponseCompressionBootstrap
{
    public static bool IsEnabled(IHostEnvironment environment) =>
        !environment.IsDevelopment() && !environment.IsEnvironment("Testing");

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
    }
}
