using Microsoft.AspNetCore.DataProtection;

namespace QueenZone.Web;

public static class DataProtectionBootstrap
{
    internal const string KeysPathConfigurationKey = "DataProtection:KeysPath";

    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (QueenZoneEnvironments.IsAutomatedTestHost(environment))
        {
            // Avoid DPAPI-backed key persistence under the service profile, which is slow
            // and unnecessary for short-lived automated test runs.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            return;
        }

        if (!QueenZoneEnvironments.IsProductionLike(environment))
        {
            return;
        }

        var configuredPath = configuration[KeysPathConfigurationKey];
        var keysPath = string.IsNullOrWhiteSpace(configuredPath)
            ? GetDefaultKeysPath()
            : configuredPath.Trim();

        if (!Path.IsPathFullyQualified(keysPath))
        {
            throw new InvalidOperationException(
                $"{KeysPathConfigurationKey} must be an absolute path outside wwwroot.");
        }

        keysPath = Path.GetFullPath(keysPath);
        if (IsWithinWebRoot(keysPath, environment.WebRootPath))
        {
            throw new InvalidOperationException(
                $"{KeysPathConfigurationKey} must be outside the read-only wwwroot directory.");
        }

        // Create the directory during startup so a bad mount or permission fails clearly,
        // before authentication or antiforgery first tries to generate a key.
        Directory.CreateDirectory(keysPath);

        services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    }

    internal static string GetDefaultKeysPath() =>
        OperatingSystem.IsWindows()
            ? @"D:\home\ASP.NET\DataProtection-Keys"
            : "/home/ASP.NET/DataProtection-Keys";

    private static bool IsWithinWebRoot(string keysPath, string? webRootPath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return false;
        }

        var webRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(webRootPath));
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return keysPath.Equals(webRoot, comparison)
            || keysPath.StartsWith(webRoot + Path.DirectorySeparatorChar, comparison);
    }
}
