using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class UgcProxyPathsTests
{
    [Fact]
    public void GetPath_builds_forum_proxy_url()
    {
        var path = UgcProxyPaths.GetPath(BlobUploadContainers.Forum, "editors/a/b.webp");
        Assert.Equal("/ugc/forum/editors/a/b.webp", path);
    }

    [Fact]
    public void ToThumbBlobName_inserts_thumb_before_extension()
    {
        Assert.Equal("editors/a/b-thumb.webp", UgcProxyPaths.ToThumbBlobName("editors/a/b.webp"));
        Assert.Equal("editors/a/b-thumb", UgcProxyPaths.ToThumbBlobName("editors/a/b"));
    }

    [Fact]
    public void GetPath_throws_for_unknown_container()
    {
        Assert.Throws<ArgumentException>(() => UgcProxyPaths.GetPath("legacy-photos", "x.webp"));
    }

    [Fact]
    public void TryParseProxySrc_accepts_absolute_url_path()
    {
        Assert.True(UgcProxyPaths.TryParseProxySrc(
            "https://www.queenzone.org/ugc/photos/members/1/a.webp",
            out var container,
            out var blobName));
        Assert.Equal(BlobUploadContainers.Photos, container);
        Assert.Equal("members/1/a.webp", blobName);
    }

    [Fact]
    public void IsProxyImageSrc_rejects_dotdot()
    {
        Assert.False(UgcProxyPaths.IsProxyImageSrc("/ugc/forum/../secret.webp"));
    }

    [Theory]
    [InlineData("/ugc/forum/editors/x.webp", true)]
    [InlineData("/ugc/articles/editors/x.webp", true)]
    [InlineData("/ugc/evil/x.webp", false)]
    [InlineData("/other/forum/x.webp", false)]
    [InlineData("../ugc/forum/x.webp", false)]
    public void IsProxyImageSrc(string src, bool expected) =>
        Assert.Equal(expected, UgcProxyPaths.IsProxyImageSrc(src));

    [Fact]
    public void TryParseProxySrc_reads_container_and_blob()
    {
        Assert.True(UgcProxyPaths.TryParseProxySrc(
            "/ugc/forum/members/abc/photo.webp",
            out var container,
            out var blobName));

        Assert.Equal(BlobUploadContainers.Forum, container);
        Assert.Equal("members/abc/photo.webp", blobName);
    }
}
