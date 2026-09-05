namespace QueenZone.Tools.Tests;

public sealed class BlobSnapshotServiceTests
{
    [Theory]
    [InlineData("/Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    [InlineData("Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    [InlineData("https://cdn.queenzone.org/Freddie_Mercury/2512001754.jpg", "freddie-mercury", "2512001754.jpg")]
    public void ParseGalleryLocation_AcceptsLegacyPathsAndHttpUrls(
        string path,
        string expectedContainer,
        string expectedName)
    {
        var (container, name) = BlobSnapshotService.ParseGalleryLocation(path);

        Assert.Equal(expectedContainer, container);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("photo.jpg")]
    public void ParseGalleryLocation_RejectsPathsWithoutContainer(string path) =>
        Assert.Throws<InvalidOperationException>(() => BlobSnapshotService.ParseGalleryLocation(path));
}
