namespace QueenZone.Data;

public enum MemberSocialChannel
{
    X,
    Instagram,
    Facebook,
    YouTube,
    TikTok,
    Bluesky,
}

public sealed record MemberSocialLink(MemberSocialChannel Channel, string Url)
{
    public string Label => MemberSocialChannels.Label(Channel);
}

public static class MemberSocialChannels
{
    public static IReadOnlyList<MemberSocialChannel> All { get; } =
    [
        MemberSocialChannel.X,
        MemberSocialChannel.Instagram,
        MemberSocialChannel.Facebook,
        MemberSocialChannel.YouTube,
        MemberSocialChannel.TikTok,
        MemberSocialChannel.Bluesky,
    ];

    public static string ToKey(MemberSocialChannel channel) => channel switch
    {
        MemberSocialChannel.X => "x",
        MemberSocialChannel.Instagram => "instagram",
        MemberSocialChannel.Facebook => "facebook",
        MemberSocialChannel.YouTube => "youtube",
        MemberSocialChannel.TikTok => "tiktok",
        MemberSocialChannel.Bluesky => "bluesky",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    public static string Label(MemberSocialChannel channel) => channel switch
    {
        MemberSocialChannel.X => "X",
        MemberSocialChannel.Instagram => "Instagram",
        MemberSocialChannel.Facebook => "Facebook",
        MemberSocialChannel.YouTube => "YouTube",
        MemberSocialChannel.TikTok => "TikTok",
        MemberSocialChannel.Bluesky => "Bluesky",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    public static bool TryParseKey(string? key, out MemberSocialChannel channel)
    {
        switch (key?.Trim().ToLowerInvariant())
        {
            case "x":
                channel = MemberSocialChannel.X;
                return true;
            case "instagram":
                channel = MemberSocialChannel.Instagram;
                return true;
            case "facebook":
                channel = MemberSocialChannel.Facebook;
                return true;
            case "youtube":
                channel = MemberSocialChannel.YouTube;
                return true;
            case "tiktok":
                channel = MemberSocialChannel.TikTok;
                return true;
            case "bluesky":
                channel = MemberSocialChannel.Bluesky;
                return true;
            default:
                channel = default;
                return false;
        }
    }
}
