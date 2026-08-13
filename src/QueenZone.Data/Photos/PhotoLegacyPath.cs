namespace QueenZone.Data;

/// <summary>
/// Builds legacy PIC_FILES_T path segments and Azure container names for gallery assets.
/// </summary>
public static class PhotoLegacyPath
{
    /// <summary>
    /// Categories whose Azure container name does not match the derived folder slug.
    /// </summary>
    private static readonly Dictionary<string, string> ContainerOverrides =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["US Queen Convention 2001"] = "us-convention-2001",
        };

    public static string CategoryFolder(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return string.Join('_', categoryName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string BlobContainerName(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        var trimmed = categoryName.Trim();
        if (ContainerOverrides.TryGetValue(trimmed, out var overrideName))
        {
            return overrideName;
        }

        return CategoryFolder(trimmed).ToLowerInvariant().Replace('_', '-');
    }

    public static string BuildLegacyPath(string categoryName, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return $"/{CategoryFolder(categoryName)}/{fileName.TrimStart('/')}";
    }
}
