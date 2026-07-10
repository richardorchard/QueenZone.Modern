using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PhotoImageUrlTests
{
    [Fact]
    public void Build_UsesPublicPicturesBaseUrl() =>
        Assert.Equal(
            "https://cdn.queenzone.org/brian-may/img-101.jpg",
            PhotoImageUrl.Build("/Brian_May/img-101.jpg"));

    [Fact]
    public void BuildBlobStorageUrl_UsesBlobEndpointAndFolderMapping() =>
        Assert.Equal(
            "https://queenzone.blob.core.windows.net/brian-may/img-101.jpg",
            PhotoImageUrl.BuildBlobStorageUrl("/Brian_May/img-101.jpg"));

    [Fact]
    public void BuildBlobStorageUrl_AllowsCustomEndpoint() =>
        Assert.Equal(
            "https://example.test/queen/img-201.jpg",
            PhotoImageUrl.BuildBlobStorageUrl("/Queen/img-201.jpg", "https://example.test"));

    [Fact]
    public void ToBlobStorageUrl_ConvertsPublicUrlToBlobEndpoint() =>
        Assert.Equal(
            "https://queenzone.blob.core.windows.net/queen/img-201.jpg",
            PhotoImageUrl.ToBlobStorageUrl("https://cdn.queenzone.org/queen/img-201.jpg"));

    [Fact]
    public void ToBlobStorageUrl_FallsBackToLegacyPathMapping() =>
        Assert.Equal(
            "https://queenzone.blob.core.windows.net/multimedia/photo.jpg",
            PhotoImageUrl.ToBlobStorageUrl("/Multimedia/photo.jpg"));

    [Fact]
    public void BuildBlobStorageUrl_HandlesSingleSegmentLegacyPath() =>
        Assert.Equal(
            "https://queenzone.blob.core.windows.net/photo.jpg",
            PhotoImageUrl.BuildBlobStorageUrl("photo.jpg"));

    [Theory]
    [InlineData("https://cdn.queenzone.org/queen/img.jpg", "queen", "img.jpg")]
    [InlineData("https://queenzone.blob.core.windows.net/multimedia/t_123.jpg", "multimedia", "t_123.jpg")]
    public void TryParseBlobLocation_ParsesContainerAndBlobName(string url, string container, string blobName)
    {
        var parsed = PhotoImageUrl.TryParseBlobLocation(url, out var actualContainer, out var actualBlobName);

        Assert.True(parsed);
        Assert.Equal(container, actualContainer);
        Assert.Equal(blobName, actualBlobName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("/Multimedia/photo.jpg")]
    [InlineData("https://queenzone.blob.core.windows.net/only-container")]
    public void TryParseBlobLocation_RejectsInvalidUrls(string url)
    {
        var parsed = PhotoImageUrl.TryParseBlobLocation(url, out _, out _);

        Assert.False(parsed);
    }
}
