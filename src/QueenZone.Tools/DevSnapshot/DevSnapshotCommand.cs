using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace QueenZone.Tools;

[ExcludeFromCodeCoverage]
internal static class DevSnapshotCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = DevSnapshotOptions.Parse(args);
            var config = DevSnapshotConfig.Load(options.ConfigPath);
            var targetSql = RequiredEnvironment("DEV_SNAPSHOT_TARGET_SQL");
            var sql = new SqlSnapshotService(config, targetSql);

            if (options.Mode == "verify")
            {
                IReadOnlyList<SnapshotBlob> verifyManifest = [];
                if (File.Exists(options.ManifestPath))
                {
                    verifyManifest = JsonSerializer.Deserialize<SnapshotBlob[]>(await File.ReadAllTextAsync(options.ManifestPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                }

                var verified = await sql.VerifyAsync(verifyManifest, requireSearchIndex: true);
                await WriteJsonAsync(options.SummaryPath, verified);
                PrintSummary(verified);
                return 0;
            }

            var sourceSql = RequiredEnvironment("DEV_SNAPSHOT_SOURCE_SQL_READONLY");
            var sourceBlob = RequiredEnvironment("DEV_SNAPSHOT_SOURCE_BLOB_READONLY");
            var targetBlob = RequiredEnvironment("DEV_SNAPSHOT_TARGET_BLOB");
            DevSnapshotSafety.EnsureBlobBoundaries(sourceBlob, targetBlob, config.TargetStorageAccount);

            await using var session = await sql.OpenCopySessionAsync(sourceSql);
            await session.PrepareSelectionsAsync();

            var blobs = new BlobSnapshotService(config, sourceBlob, targetBlob);
            var photoSelection = await blobs.SelectPhotosAsync(await session.GetPhotoCandidatesAsync());
            await session.SetSelectedPhotosAsync(photoSelection.PhotoIds);

            var manifest = photoSelection.Blobs
                .Concat(await blobs.GetForumAndEditorialBlobsAsync(session))
                .DistinctBy(blob => $"{blob.Container}/{blob.Name}", StringComparer.OrdinalIgnoreCase)
                .OrderBy(blob => blob.Container, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blob => blob.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            blobs.EnsureBudgets(manifest);

            await session.ResetTargetAsync();
            await session.CopyRowsAsync();
            await blobs.ResetTargetAndCopyAsync(manifest);
            await session.SeedSyntheticAccountsAsync(
                RequiredEnvironment("DEV_SNAPSHOT_ADMIN_PASSWORD"),
                RequiredEnvironment("DEV_SNAPSHOT_MEMBER_PASSWORD"));
            await session.FinalizeTargetAsync();

            await WriteJsonAsync(options.ManifestPath, manifest);
            var summary = await sql.VerifyAsync(manifest, requireSearchIndex: false);
            await WriteJsonAsync(options.SummaryPath, summary);
            PrintSummary(summary);
            return 0;
        }
        catch (SnapshotSizeException exception)
        {
            Console.Error.WriteLine($"Dev snapshot size guard failed: {exception.Message}");
            return 3;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Dev snapshot failed: {exception.Message}");
            return 1;
        }
    }

    private static string RequiredEnvironment(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"{name} is required.");
    }

    private static async Task WriteJsonAsync<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }

    private static void PrintSummary(SnapshotSummary summary)
    {
        Console.WriteLine($"Forum: {summary.ForumCategories} categories, {summary.ForumThreads} complete threads, {summary.ForumPosts} posts");
        Console.WriteLine($"Photos: {summary.Photos}");
        Console.WriteLine($"Members: {summary.Members} modern, {summary.LegacyUsers} legacy");
        Console.WriteLine($"Blobs: {summary.BlobCount}; gallery {summary.GalleryBlobBytes} bytes; forum {summary.ForumAttachmentBytes} bytes");
        Console.WriteLine($"Database used: {summary.DatabaseUsedMb:F1} MB");
    }
}
