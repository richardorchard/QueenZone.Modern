using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace QueenZone.Web;

public static class QueenZoneTelemetryServiceCollectionExtensions
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static IServiceCollection AddQueenZoneApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILoggingBuilder logging)
    {
        if (environment.IsEnvironment("Testing"))
        {
            return services;
        }

        var connectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return services;
        }

        var section = configuration.GetSection("ApplicationInsights");
        var tracesPerSecond = section.GetValue<double?>("TracesPerSecond") ?? 0.2;
        var enableLiveMetrics = section.GetValue<bool?>("EnableLiveMetrics") ?? false;
        var enableTraceBasedLogsSampler = section.GetValue<bool?>("EnableTraceBasedLogsSampler") ?? true;
        var exportedLogLevel = section.GetValue<LogLevel?>("ExportedLogLevel") ?? LogLevel.Warning;

        logging.AddFilter<OpenTelemetryLoggerProvider>(null, exportedLogLevel);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "QueenZone.Web",
                serviceNamespace: "QueenZone"))
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
                options.TracesPerSecond = tracesPerSecond;
                options.EnableLiveMetrics = enableLiveMetrics;
                options.EnableTraceBasedLogsSampler = enableTraceBasedLogsSampler;
            });

        return services;
    }
}
