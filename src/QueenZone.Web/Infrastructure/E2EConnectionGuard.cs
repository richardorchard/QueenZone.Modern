using System.Data.Common;

namespace QueenZone.Web;

/// <summary>
/// Fail-closed startup guard for the <see cref="QueenZoneEnvironments.E2E"/> hosting
/// environment: refuses to start unless <c>ConnectionStrings:QueenZoneLegacy</c> points at the
/// disposable local SQL Express mirror (<see cref="ExpectedDatabase"/>). Mirrors
/// <c>scripts/Assert-SqlExpressMirrorConnection.ps1</c> in process so a misconfigured nightly
/// run can never point the write-heavy Playwright suite at Azure SQL.
/// </summary>
public static class E2EConnectionGuard
{
    public const string ExpectedDatabase = "queenzone_legacy_sync";

    private static readonly string[] ServerKeys = ["Server", "Data Source", "Address", "Addr", "Network Address"];
    private static readonly string[] DatabaseKeys = ["Database", "Initial Catalog"];

    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="connectionString"/> is empty, targets a database other than
    /// <see cref="ExpectedDatabase"/>, or targets a server outside the local SQL Express
    /// allowlist.
    /// </exception>
    public static void EnsureSafe(string? connectionString, string? machineName = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "E2E requires ConnectionStrings:QueenZoneLegacy to point at the disposable SQL " +
                $"Express mirror ('{ExpectedDatabase}'). An empty connection string is a " +
                "misconfiguration, not a silent fallback to in-memory data.");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var database = GetValue(builder, DatabaseKeys);
        if (!string.Equals(database, ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"E2E refused database '{database}'. Expected the disposable mirror '{ExpectedDatabase}'.");
        }

        var server = GetValue(builder, ServerKeys);
        var allowedServers = GetAllowedServers(machineName);
        if (!allowedServers.Contains(RemovePort(server), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"E2E refused server '{server}'. Use the local SQL Express mirror (or its LAN " +
                "address for the Mac runner); Azure SQL and other remote servers are blocked.");
        }
    }

    private static string GetValue(DbConnectionStringBuilder builder, string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value) && value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<string> GetAllowedServers(string? machineName)
    {
        var allowed = new List<string>
        {
            @"localhost\SQLEXPRESS",
            @".\SQLEXPRESS",
            @"(local)\SQLEXPRESS",
            @"127.0.0.1\SQLEXPRESS",
            // The Mac runner has no Integrated Security path to the Windows box, so it reaches
            // the same disposable mirror over the LAN with a SQL login instead (see
            // docs/agent-bitwarden-secrets.md and scripts/Assert-SqlExpressMirrorConnection.ps1,
            // which this guard mirrors). Still the local mirror, not a remote server.
            "glory11",
            @"glory11\SQLEXPRESS",
        };

        var effectiveMachineName = machineName ?? Environment.MachineName;
        if (!string.IsNullOrWhiteSpace(effectiveMachineName))
        {
            allowed.Add($@"{effectiveMachineName}\SQLEXPRESS");
        }

        var lanAddress = Environment.GetEnvironmentVariable("SQLEXPRESS_LAN_ADDRESS");
        if (!string.IsNullOrWhiteSpace(lanAddress))
        {
            allowed.Add(RemovePort(lanAddress));
        }

        return allowed;
    }

    // Strips a trailing ",<port>" (e.g. "192.168.1.237,1433") so LAN addresses compare the same
    // way regardless of whether a port was supplied.
    private static string RemovePort(string value) =>
        value.Contains(',') ? value[..value.IndexOf(',')] : value;
}
