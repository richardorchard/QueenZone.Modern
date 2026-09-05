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
}
