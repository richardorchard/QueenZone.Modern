using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class MemberSocialLinkUrlTests
{
    [Theory]
    [InlineData(MemberSocialChannel.X, "queen", "https://x.com/queen")]
    [InlineData(MemberSocialChannel.X, "@queen", "https://x.com/queen")]
    [InlineData(MemberSocialChannel.X, "https://x.com/queen", "https://x.com/queen")]
    [InlineData(MemberSocialChannel.X, "http://twitter.com/queen?utm_source=share", "https://x.com/queen")]
    [InlineData(MemberSocialChannel.X, "https://www.twitter.com/queen/", "https://x.com/queen")]
    [InlineData(MemberSocialChannel.Instagram, "queen", "https://www.instagram.com/queen")]
    [InlineData(MemberSocialChannel.Instagram, "@queen.official", "https://www.instagram.com/queen.official")]
    [InlineData(MemberSocialChannel.Instagram, "https://instagram.com/queen", "https://www.instagram.com/queen")]
    [InlineData(MemberSocialChannel.Facebook, "queen", "https://www.facebook.com/queen")]
    [InlineData(MemberSocialChannel.Facebook, "https://fb.com/queen?locale=en", "https://www.facebook.com/queen")]
    [InlineData(MemberSocialChannel.Facebook, "https://www.facebook.com/pages/Queen/12345", "https://www.facebook.com/pages/Queen/12345")]
    [InlineData(MemberSocialChannel.Facebook, "https://facebook.com/profile.php?id=12345&sk=about", "https://www.facebook.com/profile.php?id=12345")]
    [InlineData(MemberSocialChannel.YouTube, "QueenOfficial", "https://www.youtube.com/@QueenOfficial")]
    [InlineData(MemberSocialChannel.YouTube, "@QueenOfficial", "https://www.youtube.com/@QueenOfficial")]
    [InlineData(MemberSocialChannel.YouTube, "https://youtube.com/@QueenOfficial", "https://www.youtube.com/@QueenOfficial")]
    [InlineData(MemberSocialChannel.YouTube, "https://www.youtube.com/channel/UC1234567890123456789012", "https://www.youtube.com/channel/UC1234567890123456789012")]
    [InlineData(MemberSocialChannel.YouTube, "https://www.youtube.com/c/QueenOfficial", "https://www.youtube.com/c/QueenOfficial")]
    [InlineData(MemberSocialChannel.YouTube, "https://www.youtube.com/user/QueenOfficial", "https://www.youtube.com/user/QueenOfficial")]
    [InlineData(MemberSocialChannel.TikTok, "queen", "https://www.tiktok.com/@queen")]
    [InlineData(MemberSocialChannel.TikTok, "@queen", "https://www.tiktok.com/@queen")]
    [InlineData(MemberSocialChannel.TikTok, "https://tiktok.com/@queen", "https://www.tiktok.com/@queen")]
    [InlineData(MemberSocialChannel.Bluesky, "alice", "https://bsky.app/profile/alice.bsky.social")]
    [InlineData(MemberSocialChannel.Bluesky, "@alice.bsky.social", "https://bsky.app/profile/alice.bsky.social")]
    [InlineData(MemberSocialChannel.Bluesky, "https://bsky.app/profile/alice.bsky.social", "https://bsky.app/profile/alice.bsky.social")]
    [InlineData(MemberSocialChannel.Bluesky, "https://alice.bsky.social", "https://bsky.app/profile/alice.bsky.social")]
    public void TryNormalize_AcceptsHandleOrUrl(MemberSocialChannel channel, string input, string expected)
    {
        Assert.True(MemberSocialLinkUrl.TryNormalize(channel, input, out var url, out var error));
        Assert.Null(error);
        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_Empty_ClearsChannel(string? input)
    {
        Assert.True(MemberSocialLinkUrl.TryNormalize(MemberSocialChannel.X, input, out var url, out var error));
        Assert.Null(error);
        Assert.Null(url);
    }

    [Theory]
    [InlineData(MemberSocialChannel.X, "https://instagram.com/queen")]
    [InlineData(MemberSocialChannel.Instagram, "https://x.com/queen")]
    [InlineData(MemberSocialChannel.Facebook, "https://tiktok.com/@queen")]
    [InlineData(MemberSocialChannel.YouTube, "https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData(MemberSocialChannel.YouTube, "https://youtu.be/dQw4w9WgXcQ")]
    [InlineData(MemberSocialChannel.TikTok, "https://www.tiktok.com/video/123")]
    [InlineData(MemberSocialChannel.Bluesky, "https://example.com/alice")]
    [InlineData(MemberSocialChannel.X, "javascript:alert(1)")]
    [InlineData(MemberSocialChannel.Instagram, "javascript:alert(1)")]
    [InlineData(MemberSocialChannel.X, "https://user:pass@x.com/queen")]
    [InlineData(MemberSocialChannel.X, "???")]
    [InlineData(MemberSocialChannel.X, "@@@")]
    [InlineData(MemberSocialChannel.X, "thisnameistoolongforx")]
    [InlineData(MemberSocialChannel.X, "https://x.com/queen/status/1")]
    [InlineData(MemberSocialChannel.Instagram, "https://instagram.com/p/abc")]
    [InlineData(MemberSocialChannel.Facebook, "https://facebook.com/watch")]
    [InlineData(MemberSocialChannel.YouTube, "https://www.youtube.com/shorts/abc")]
    [InlineData(MemberSocialChannel.Bluesky, "https://evil.bsky.social.example.com")]
    public void TryNormalize_RejectsInvalidInput(MemberSocialChannel channel, string input)
    {
        Assert.False(MemberSocialLinkUrl.TryNormalize(channel, input, out var url, out var error));
        Assert.Null(url);
        Assert.Equal(MemberSocialLinkUrl.InvalidValueMessage, error);
    }

    [Fact]
    public void TryNormalize_RejectsOverlongInput()
    {
        var input = new string('a', MemberSocialLinkUrl.MaxInputLength + 1);

        Assert.False(MemberSocialLinkUrl.TryNormalize(MemberSocialChannel.X, input, out var url, out var error));
        Assert.Null(url);
        Assert.Equal(MemberSocialLinkUrl.InvalidValueMessage, error);
    }

    [Theory]
    [InlineData(MemberSocialChannel.X, "x")]
    [InlineData(MemberSocialChannel.Instagram, "instagram")]
    [InlineData(MemberSocialChannel.Facebook, "facebook")]
    [InlineData(MemberSocialChannel.YouTube, "youtube")]
    [InlineData(MemberSocialChannel.TikTok, "tiktok")]
    [InlineData(MemberSocialChannel.Bluesky, "bluesky")]
    public void ChannelKeys_RoundTrip(MemberSocialChannel channel, string key)
    {
        Assert.Equal(key, MemberSocialChannels.ToKey(channel));
        Assert.True(MemberSocialChannels.TryParseKey(key, out var parsed));
        Assert.Equal(channel, parsed);
        Assert.False(string.IsNullOrWhiteSpace(MemberSocialChannels.Label(channel)));
    }

    [Fact]
    public void TryParseKey_RejectsUnknown()
    {
        Assert.False(MemberSocialChannels.TryParseKey("myspace", out _));
        Assert.False(MemberSocialChannels.TryParseKey(null, out _));
    }

    [Fact]
    public void UnknownChannel_IsRejected()
    {
        const MemberSocialChannel unknown = (MemberSocialChannel)99;
        Assert.Throws<ArgumentOutOfRangeException>(() => MemberSocialChannels.ToKey(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => MemberSocialChannels.Label(unknown));
        Assert.False(MemberSocialLinkUrl.TryNormalize(unknown, "queen", out var handleUrl, out _));
        Assert.Null(handleUrl);
        Assert.False(MemberSocialLinkUrl.TryNormalize(unknown, "https://x.com/queen", out var url, out _));
        Assert.Null(url);
    }
}
