using Microsoft.Data.SqlClient;

namespace QueenZone.Tools.Tests;

public sealed class DevSnapshotOptionsTests
{
    [Fact]
    public void Parse_requires_mode_and_config()
    {
        Assert.Throws<ArgumentException>(() => DevSnapshotOptions.Parse([]));
        Assert.Throws<ArgumentException>(() => DevSnapshotOptions.Parse(["copy"]));
        Assert.Throws<ArgumentException>(() => DevSnapshotOptions.Parse(["other", "--config", "x.json"]));
    }

    [Fact]
    public void Parse_accepts_copy_outputs()
    {
        var options = DevSnapshotOptions.Parse(
            ["copy", "--config", "config.json", "--manifest", "manifest.json", "--summary", "summary.json"]);

        Assert.Equal("copy", options.Mode);
        Assert.Equal("config.json", options.ConfigPath);
        Assert.Equal("manifest.json", options.ManifestPath);
        Assert.Equal("summary.json", options.SummaryPath);
    }

    [Fact]
    public void Parse_rejects_unknown_or_incomplete_arguments()
    {
        Assert.Throws<ArgumentException>(() => DevSnapshotOptions.Parse(["verify", "--config"]));
        Assert.Throws<ArgumentException>(() => DevSnapshotOptions.Parse(["verify", "--config", "x", "--unknown"]));
    }

    [Fact]
    public void Config_rejects_wrong_boundaries_and_table_overlap()
    {
        Assert.Contains("source database", LoadFailure("wrong", "queenzone-dev-db", "queenzonedev", ["NEWS_T"], ["Q_PM_T"]), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("target database", LoadFailure("queenzone-db", "wrong", "queenzonedev", ["NEWS_T"], ["Q_PM_T"]), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("storage account", LoadFailure("queenzone-db", "queenzone-dev-db", "wrong", ["NEWS_T"], ["Q_PM_T"]), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cannot be public", LoadFailure("queenzone-db", "queenzone-dev-db", "queenzonedev", ["NEWS_T"], ["NEWS_T"]), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Config_requires_positive_record_limits()
    {
        var path = WriteConfig("queenzone-db", "queenzone-dev-db", "queenzonedev", ["NEWS_T"], ["Q_PM_T"]);
        try
        {
            var config = DevSnapshotConfig.Load(path);
            Assert.Equal(500, config.ForumThreadCount);
            Assert.Equal(500, config.NewsArticleCount);
            Assert.Equal(500, config.ArticleCount);
            Assert.Equal(100, config.PhotosPerCategory);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Source_sql_builder_forces_read_intent_without_changing_database()
    {
        var result = DevSnapshotSafety.BuildReadOnlySourceConnectionString(
            "Server=example.database.windows.net;Database=queenzone-db;User ID=reader;Password=secret;Encrypt=True");
        var builder = new SqlConnectionStringBuilder(result);

        Assert.Equal("queenzone-db", builder.InitialCatalog);
        Assert.Equal(ApplicationIntent.ReadOnly, builder.ApplicationIntent);
        Assert.Equal("QueenZone.DevSnapshot.ReadOnly", builder.ApplicationName);
    }

    [Fact]
    public void Blob_boundaries_require_read_list_sas_and_exact_dev_target()
    {
        var source = "BlobEndpoint=https://source.blob.core.windows.net;SharedAccessSignature=sv=2025-01-05&ss=b&srt=sco&sp=rl&se=2030-01-01T00%3A00%3A00Z&sig=fake";
        var key = Convert.ToBase64String(new byte[32]);
        var target = $"DefaultEndpointsProtocol=https;AccountName=queenzonedev;AccountKey={key};EndpointSuffix=core.windows.net";

        DevSnapshotSafety.EnsureBlobBoundaries(source, target, "queenzonedev");
        Assert.Throws<InvalidOperationException>(() =>
            DevSnapshotSafety.EnsureBlobBoundaries(source.Replace("sp=rl", "sp=rwl", StringComparison.Ordinal), target, "queenzonedev"));
        Assert.Throws<InvalidOperationException>(() =>
            DevSnapshotSafety.EnsureBlobBoundaries(target, target, "queenzonedev"));
        Assert.Throws<InvalidOperationException>(() =>
            DevSnapshotSafety.EnsureBlobBoundaries(source, target, "other"));
    }

    private static string LoadFailure(
        string source,
        string target,
        string storage,
        string[] publicTables,
        string[] forbiddenTables)
    {
        var path = WriteConfig(source, target, storage, publicTables, forbiddenTables);
        try
        {
            return Assert.Throws<InvalidOperationException>(() => DevSnapshotConfig.Load(path)).Message;
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteConfig(
        string source,
        string target,
        string storage,
        string[] publicTables,
        string[] forbiddenTables)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dev-snapshot-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, $$"""
            {
              "sourceDatabase": "{{source}}",
              "targetDatabase": "{{target}}",
              "targetStorageAccount": "{{storage}}",
              "forumThreadCount": 500,
              "newsArticleCount": 500,
              "articleCount": 500,
              "photosPerCategory": 100,
              "galleryBudgetBytes": 524288000,
              "forumAttachmentBudgetBytes": 524288000,
              "databaseMaximumUsedMb": 1536,
              "publicTables": ["{{string.Join("\",\"", publicTables)}}"],
              "forbiddenTables": ["{{string.Join("\",\"", forbiddenTables)}}"]
            }
            """);
        return path;
    }
}
