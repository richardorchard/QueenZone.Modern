using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AudioDurationTests
{
    [Fact]
    public void TryGetSeconds_Mpeg_UsesMp3Duration()
    {
        var prefix = Mp3DurationTests.CreateMpeg1Layer3Header(bitrateIndex: 9);
        Assert.Equal(1, AudioDuration.TryGetSeconds("audio/mpeg", prefix, 16_000));
        Assert.Equal(1, AudioDuration.TryGetSeconds("audio/mp3", prefix, 16_000));
    }

    [Fact]
    public void TryGetSeconds_Flac_UsesStreamInfo()
    {
        var prefix = CreateFlacStreamInfo(sampleRate: 44_100, totalSamples: 44_100);
        Assert.Equal(1, AudioDuration.TryGetSeconds("audio/flac", prefix, prefix.Length));
        Assert.Equal(1, AudioDuration.TryGetSeconds("audio/x-flac", prefix, prefix.Length));
    }

    [Fact]
    public void TryGetSeconds_Flac_RoundsAwayFromZero()
    {
        var prefix = CreateFlacStreamInfo(sampleRate: 44_100, totalSamples: 66_150);
        Assert.Equal(2, AudioDuration.TryGetSeconds("audio/flac", prefix, prefix.Length));
    }

    [Fact]
    public void TryGetSeconds_ReturnsNull_ForUnknownTypeOrGarbage()
    {
        var mpeg = Mp3DurationTests.CreateMpeg1Layer3Header(9);
        Assert.Null(AudioDuration.TryGetSeconds(null, mpeg, 16_000));
        Assert.Null(AudioDuration.TryGetSeconds("audio/mp4", mpeg, 16_000));
        Assert.Null(AudioDuration.TryGetSeconds("audio/mpeg", "not-an-mp3"u8.ToArray(), 16_000));
        Assert.Null(AudioDuration.TryGetSeconds("audio/flac", "not-flac"u8.ToArray(), 100));
        Assert.Null(AudioDuration.TryGetSeconds("audio/flac", [], 100));
        Assert.Null(FlacDuration.TryGetSeconds("fLaC"u8.ToArray()));
    }

    [Fact]
    public void TryGetSeconds_ReturnsNull_WhenFlacStreamInfoIsUnusable()
    {
        var wrongBlock = CreateFlacStreamInfo(44_100, 44_100);
        wrongBlock[4] = 0x04;
        Assert.Null(FlacDuration.TryGetSeconds(wrongBlock));

        var shortBlock = CreateFlacStreamInfo(44_100, 44_100);
        shortBlock[5] = 0;
        shortBlock[6] = 0;
        shortBlock[7] = 10;
        Assert.Null(FlacDuration.TryGetSeconds(shortBlock));

        Assert.Null(FlacDuration.TryGetSeconds(CreateFlacStreamInfo(sampleRate: 0, totalSamples: 44_100)));
        Assert.Null(FlacDuration.TryGetSeconds(CreateFlacStreamInfo(sampleRate: 44_100, totalSamples: 0)));
    }

    [Fact]
    public void TryGetSeconds_NeverThrows()
    {
        Assert.Null(AudioDuration.TryGetSeconds("audio/mpeg", [], -1));
        Assert.Null(AudioDuration.TryGetSeconds("audio/flac", [0x00], long.MaxValue));
    }

    internal static byte[] CreateFlacStreamInfo(int sampleRate, long totalSamples)
    {
        var prefix = new byte[42];
        "fLaC"u8.CopyTo(prefix);
        prefix[4] = 0x80;
        prefix[7] = 34;
        prefix[18] = (byte)(sampleRate >> 12);
        prefix[19] = (byte)(sampleRate >> 4);
        prefix[20] = (byte)((sampleRate & 0x0F) << 4);
        prefix[21] = (byte)((totalSamples >> 32) & 0x0F);
        prefix[22] = (byte)(totalSamples >> 24);
        prefix[23] = (byte)(totalSamples >> 16);
        prefix[24] = (byte)(totalSamples >> 8);
        prefix[25] = (byte)totalSamples;
        return prefix;
    }
}
