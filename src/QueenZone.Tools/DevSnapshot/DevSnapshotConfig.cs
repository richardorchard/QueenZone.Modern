using System.Text.Json;

namespace QueenZone.Tools;

internal sealed class DevSnapshotConfig
{
    public string SourceDatabase { get; init; } = "queenzone-db";

    public string TargetDatabase { get; init; } = "queenzone-dev-db";

    public string TargetStorageAccount { get; init; } = "queenzonedev";

    public int ForumThreadCount { get; init; } = 500;

    public int NewsArticleCount { get; init; } = 500;

    public int ArticleCount { get; init; } = 500;

    public int PhotosPerCategory { get; init; } = 100;

    public long GalleryBudgetBytes { get; init; } = 500L * 1024 * 1024;

    public long ForumAttachmentBudgetBytes { get; init; } = 500L * 1024 * 1024;

    public decimal DatabaseMaximumUsedMb { get; init; } = 1536m;

    public IReadOnlyList<string> PublicTables { get; init; } = [];

    public IReadOnlyList<string> ForbiddenTables { get; init; } = [];

    public static DevSnapshotConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<DevSnapshotConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Snapshot configuration is empty.");
        config.Validate();
        return config;
    }

    private void Validate()
    {
        if (!string.Equals(SourceDatabase, "queenzone-db", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The source database must be queenzone-db.");
        }

        if (!string.Equals(TargetDatabase, "queenzone-dev-db", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The target database must be queenzone-dev-db.");
        }

        if (!string.Equals(TargetStorageAccount, "queenzonedev", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The target storage account must be queenzonedev.");
        }

        if (ForumThreadCount < 1 || NewsArticleCount < 1 || ArticleCount < 1 || PhotosPerCategory < 1)
        {
            throw new InvalidOperationException("Snapshot record limits must be positive.");
        }

        if (GalleryBudgetBytes < 1 || ForumAttachmentBudgetBytes < 1 || DatabaseMaximumUsedMb <= 0)
        {
            throw new InvalidOperationException("Snapshot size limits must be positive.");
        }

        if (PublicTables.Count == 0 || ForbiddenTables.Count == 0)
        {
            throw new InvalidOperationException("Public and forbidden table lists are required.");
        }

        var overlap = PublicTables.Intersect(ForbiddenTables, StringComparer.OrdinalIgnoreCase).ToArray();
        if (overlap.Length > 0)
        {
            throw new InvalidOperationException($"Tables cannot be public and forbidden: {string.Join(", ", overlap)}");
        }
    }
}
