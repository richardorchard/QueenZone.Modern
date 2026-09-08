using Azure.Storage.Blobs;
using Microsoft.Data.SqlClient;

namespace QueenZone.Tools;

internal static class DevSnapshotSafety
{
    private static readonly char[] ForbiddenBlobPermissions = ['w', 'd', 'a', 'c', 't', 'm', 'e', 'o', 'p', 'i'];

    public static string BuildReadOnlySourceConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ApplicationName = "QueenZone.DevSnapshot.ReadOnly",
        };
        return builder.ConnectionString;
    }

    public static void EnsureBlobBoundaries(
        string sourceConnectionString,
        string targetConnectionString,
        string targetAccount)
    {
        var sourceUri = GetBlobEndpoint(sourceConnectionString);
        var target = new BlobServiceClient(targetConnectionString);
        if (!string.Equals(target.AccountName, targetAccount, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Target blob account must be {targetAccount}.");
        }

        if (sourceUri.Host.Equals(target.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and target blob accounts must differ.");
        }

        var values = ParseConnectionString(sourceConnectionString);
        if (values.ContainsKey("AccountKey") || !values.TryGetValue("SharedAccessSignature", out var sas))
        {
            throw new InvalidOperationException("Source blob access must use a read/list-only account SAS, not an account key.");
        }

        var query = ParseQuery(sas);
        if (!query.TryGetValue("sp", out var permissions)
            || !permissions.Contains('r')
            || !permissions.Contains('l')
            || permissions.Any(permission => ForbiddenBlobPermissions.Contains(permission)))
        {
            throw new InvalidOperationException("Source blob SAS must grant read and list only.");
        }
    }

    private static Uri GetBlobEndpoint(string connectionString) =>
        new BlobServiceClient(connectionString).Uri;

    private static Dictionary<string, string> ParseConnectionString(string connectionString) =>
        connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0].Trim(), part => part[1].Trim(), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string> ParseQuery(string value)
    {
        var query = value.TrimStart('?');
        return query.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => Uri.UnescapeDataString(part[1]), StringComparer.OrdinalIgnoreCase);
    }
}
