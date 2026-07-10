using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SongFileUrlTests
{
    [Fact]
    public void Build_PrependsSongfilesContainerBaseUrl() =>
        Assert.Equal(
            "https://cdn.queenzone.org/songfiles/2014417798057369.mp3",
            SongFileUrl.Build("2014417798057369.mp3"));

    [Fact]
    public void Build_TrimsLeadingSlash() =>
        Assert.Equal(
            "https://cdn.queenzone.org/songfiles/2014417798057369.mp3",
            SongFileUrl.Build("/2014417798057369.mp3"));
}
