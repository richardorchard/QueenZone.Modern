namespace QueenZone.Data;

/// <summary>
/// Builds legacy PIC_FILES_T path segments and Azure container names for gallery assets.
/// </summary>
public static class PhotoLegacyPath
{
    public static string CategoryFolder(string categoryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);
        return string.Join('_', categoryName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string BlobContainerName(string categoryName) =>
        CategoryFolder(categoryName).ToLowerInvariant().Replace('_', '-');

    public static string BuildLegacyPath(string categoryName, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return $"/{CategoryFolder(categoryName)}/{fileName.TrimStart('/')}";
    }
}
