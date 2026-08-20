using System.Reflection;

namespace QueenZone.Web;

/// <summary>
/// Development-only local secrets loading. WebApplicationFactory tests that force
/// <c>Development</c> (for cache-header behaviour) must not pick up a machine's
/// <c>appsettings.Local.json</c> or a half-configured Analytics env pair.
/// </summary>
public static class QueenZoneDevelopmentHost
{
    public const string SkipLocalSettingsKey = "QueenZone:SkipDevelopmentLocalSettings";

    public static bool ShouldLoadLocalSettings(
        IHostEnvironment environment,
        IConfiguration configuration,
        string? entryAssemblyName)
    {
        if (!environment.IsDevelopment())
        {
            return false;
        }

        if (configuration.GetValue(SkipLocalSettingsKey, false))
        {
            return false;
        }

        return !IsTestProcess(entryAssemblyName);
    }

    public static bool IsTestProcess(string? entryAssemblyName)
    {
        if (string.IsNullOrWhiteSpace(entryAssemblyName))
        {
            return false;
        }

        return entryAssemblyName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase)
            || entryAssemblyName.Contains("TestRunner", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetEntryAssemblyName() =>
        Assembly.GetEntryAssembly()?.GetName().Name;

    /// <summary>
    /// CreateBuilder already loaded process environment variables. If only one of the GA Data
    /// API settings is present, pin both empty so Development test hosts can start.
    /// </summary>
    public static void NeutralizeIncompleteAnalytics(IConfigurationBuilder configuration)
    {
        var preview = configuration.Build();
        var hasPropertyId = !string.IsNullOrWhiteSpace(preview["Analytics:GoogleAnalyticsPropertyId"]);
        var hasServiceAccount = !string.IsNullOrWhiteSpace(preview["Analytics:GoogleAnalyticsServiceAccountJson"]);
        if (hasPropertyId == hasServiceAccount)
        {
            return;
        }

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Analytics:GoogleAnalyticsPropertyId"] = "",
            ["Analytics:GoogleAnalyticsServiceAccountJson"] = "",
        });
    }
}
