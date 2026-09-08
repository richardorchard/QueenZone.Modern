using Azure.Storage.Blobs;

namespace QueenZone.Tools.Tests;

public sealed class BlobSnapshotServiceTests
{
    [Theory]
    [InlineData("/Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    [InlineData("Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    [InlineData("https://cdn.queenzone.org/Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    public void TryParseGalleryLocation_AcceptsLegacyPathsAndHttpUrls(
        string path,
        string expectedContainer,
        string expectedName)
    {
        var parsed = BlobSnapshotService.TryParseGalleryLocation(path, out var container, out var name);

        Assert.True(parsed);
        Assert.Equal(expectedContainer, container);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("photo.jpg")]
    [InlineData("/fan-pics/")]
    [InlineData("ftp://example.test/freddie/image.jpg")]
    public void TryParseGalleryLocation_RejectsPathsWithoutBlobNames(string path) =>
        Assert.False(BlobSnapshotService.TryParseGalleryLocation(path, out _, out _));

    [Fact]
    public void ParseMissingForumBlobReference_ParsesLegacyPostId()
    {
        var result = BlobSnapshotService.ParseMissingForumBlobReference("ModernForumPost:43558");

        Assert.Equal(43558, result.LegacyPostId);
        Assert.Null(result.AttachmentId);
    }

    [Fact]
    public void ParseMissingForumBlobReference_ParsesModernAttachmentId()
    {
        var id = Guid.NewGuid();

        var result = BlobSnapshotService.ParseMissingForumBlobReference($"ForumPostAttachments:{id}");

        Assert.Null(result.LegacyPostId);
        Assert.Equal(id, result.AttachmentId);
    }

    [Fact]
    public void ParseMissingForumBlobReference_RejectsUnknownSources() =>
        Assert.Throws<InvalidOperationException>(
            () => BlobSnapshotService.ParseMissingForumBlobReference("NEWS_T:123"));

    [Fact]
    public void ParseMissingEditorialBlobReference_ParsesLegacyNewsId()
    {
        var result = BlobSnapshotService.ParseMissingEditorialBlobReference("NEWS_T:7023");

        Assert.Equal(7023, result.LegacyNewsId);
        Assert.Null(result.EditorialArticleId);
        Assert.False(result.IsLive);
    }

    [Theory]
    [InlineData("EditorialArticles:", false)]
    [InlineData("EditorialArticles-live:", true)]
    public void ParseMissingEditorialBlobReference_ParsesEditorialArticleId(string prefix, bool expectedIsLive)
    {
        var id = Guid.NewGuid();

        var result = BlobSnapshotService.ParseMissingEditorialBlobReference($"{prefix}{id}");

        Assert.Null(result.LegacyNewsId);
        Assert.Equal(id, result.EditorialArticleId);
        Assert.Equal(expectedIsLive, result.IsLive);
    }

    [Fact]
    public void ParseMissingEditorialBlobReference_RejectsUnknownSources() =>
        Assert.Throws<InvalidOperationException>(
            () => BlobSnapshotService.ParseMissingEditorialBlobReference("ModernForumPost:43558"));

    [Fact]
    public void IsCurrent_RequiresMatchingBytesSourceVersionAndFormat()
    {
        var blob = new SnapshotBlob("gallery", "photo.jpg", "gallery", 123, "PIC_FILES_T:1", "etag-1");
        var metadata = new Dictionary<string, string>
        {
            ["qzsourceversion"] = BlobSnapshotService.SourceVersion(blob.SourceETag),
            ["qzsnapshotformat"] = "1",
        };

        Assert.True(BlobSnapshotService.IsCurrent(blob, 123, metadata));
        Assert.False(BlobSnapshotService.IsCurrent(blob, 124, metadata));
        Assert.False(BlobSnapshotService.IsCurrent(blob with { SourceETag = "etag-2" }, 123, metadata));
        Assert.False(BlobSnapshotService.IsCurrent(blob, 123, new Dictionary<string, string>()));
    }

    [Fact]
    public void CreateSourceReadUri_GeneratesReadOnlyBlobSasFromSharedKey()
    {
        var accountKey = Convert.ToBase64String(new byte[32]);
        var service = new BlobServiceClient(
            $"DefaultEndpointsProtocol=https;AccountName=sourceaccount;AccountKey={accountKey};EndpointSuffix=core.windows.net");
        var blob = service.GetBlobContainerClient("gallery").GetBlobClient("photo.jpg");

        var result = BlobSnapshotService.CreateSourceReadUri(blob, new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal("sourceaccount.blob.core.windows.net", result.Host);
        Assert.Contains("sp=r", result.Query, StringComparison.Ordinal);
        Assert.Contains("sr=b", result.Query, StringComparison.Ordinal);
        Assert.Contains("sig=", result.Query, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateSourceReadUri_PreservesExistingSas()
    {
        var blob = new BlobClient(new Uri(
            "https://sourceaccount.blob.core.windows.net/gallery/photo.jpg?sv=2026-01-01&sp=r&sig=existing"));

        var result = BlobSnapshotService.CreateSourceReadUri(blob, DateTimeOffset.UtcNow);

        Assert.Equal(blob.Uri, result);
    }
}
