using System.Text.Json;

namespace QueenZone.Tools;

internal sealed class ToolsLocalSettings
{
    public string? QueenZoneLegacyLive { get; init; }

    public string? BlobStorage { get; init; }

    public static ToolsLocalSettings? TryLoad(string? explicitPath = null)
    {
        var path = explicitPath ?? FindLocalSettingsPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(path);
            var readerOptions = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            };
            using var document = JsonDocument.Parse(stream, readerOptions);
            if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
            {
                return null;
            }

            return new ToolsLocalSettings
            {
                QueenZoneLegacyLive = TryGetString(connectionStrings, "QueenZoneLegacyLive"),
                BlobStorage = TryGetString(connectionStrings, "BlobStorage"),
            };
        }
        catch (JsonException)
        {
            // Local developer settings are often JSONC / hand-edited; ignore invalid files.
            return null;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? FindLocalSettingsPath()
    {
        const string relativePath = "src/QueenZone.Web/appsettings.Local.json";
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
