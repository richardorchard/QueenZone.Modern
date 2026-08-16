using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SongFileUrlTests
{
    [Fact]
    public void ContainerName_IsSongfiles() =>
        Assert.Equal("songfiles", SongFileUrl.ContainerName);

    [Fact]
    public void GetBlobName_ReturnsBareFilename() =>
        Assert.Equal("2014417798057369.mp3", SongFileUrl.GetBlobName("2014417798057369.mp3"));

    [Fact]
    public void GetBlobName_TrimsLeadingSlash() =>
        Assert.Equal("2014417798057369.mp3", SongFileUrl.GetBlobName("/2014417798057369.mp3"));

    [Fact]
    public void GetBlobName_ExtractsFilenameFromAbsoluteUrl() =>
        Assert.Equal(
            "2014417798057369.mp3",
            SongFileUrl.GetBlobName("https://cdn2.queenzone.org/songfiles/2014417798057369.mp3"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../secret.mp3")]
    [InlineData("folder/file.mp3")]
    [InlineData("folder\\file.mp3")]
    public void IsSafeBlobName_RejectsEmptyAndPathShapedValues(string? fileName) =>
        Assert.False(SongFileUrl.IsSafeBlobName(fileName));

    [Fact]
    public void IsSafeBlobName_AcceptsBareMp3Name() =>
        Assert.True(SongFileUrl.IsSafeBlobName("2014417798057369.mp3"));
}
