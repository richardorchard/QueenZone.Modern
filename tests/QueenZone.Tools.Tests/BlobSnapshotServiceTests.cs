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
}
