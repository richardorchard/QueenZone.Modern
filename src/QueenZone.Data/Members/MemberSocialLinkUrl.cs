using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace QueenZone.Data;

public static partial class MemberSocialLinkUrl
{
    public const int MaxInputLength = 200;

    public const int MaxUrlLength = 256;

    public const string InvalidValueMessage = "Enter a handle or https profile URL for this network.";

    public static bool TryNormalize(
        MemberSocialChannel channel,
        string? input,
        out string? canonicalUrl,
        [NotNullWhen(false)] out string? error)
    {
        canonicalUrl = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > MaxInputLength || LooksLikeUnsafeScheme(trimmed))
        {
            error = InvalidValueMessage;
            return false;
        }

        if (LooksLikeAbsoluteUrl(trimmed))
        {
            return TryNormalizeAbsoluteUrl(channel, trimmed, out canonicalUrl, out error);
        }

        return TryNormalizeHandle(channel, trimmed, out canonicalUrl, out error);
    }

    private static bool LooksLikeUnsafeScheme(string value)
    {
        var colon = value.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        var scheme = value[..colon];
        return scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("data", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("vbscript", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("file", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeAbsoluteUrl(string value)
    {
        return value.Contains("://", StringComparison.Ordinal)
            || value.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryNormalizeHandle(
        MemberSocialChannel channel,
        string input,
        out string? canonicalUrl,
        [NotNullWhen(false)] out string? error)
    {
        canonicalUrl = null;
        var handle = StripLeadingAt(input);
        if (!IsValidHandle(channel, handle))
        {
            error = InvalidValueMessage;
            return false;
        }

        canonicalUrl = BuildFromHandle(channel, handle);
        error = null;
        return true;
    }

    private static bool TryNormalizeAbsoluteUrl(
        MemberSocialChannel channel,
        string input,
        out string? canonicalUrl,
        [NotNullWhen(false)] out string? error)
    {
        canonicalUrl = null;
        error = InvalidValueMessage;

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        var host = NormalizeHost(uri.Host);
        if (!HostIsAllowed(channel, host))
        {
            return false;
        }

        if (!TryCanonicalFromUri(channel, host, uri, out canonicalUrl))
        {
            return false;
        }

        if (canonicalUrl.Length > MaxUrlLength)
        {
            canonicalUrl = null;
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryCanonicalFromUri(
        MemberSocialChannel channel,
        string host,
        Uri uri,
        [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        var segments = GetPathSegments(uri);
        return channel switch
        {
            MemberSocialChannel.X => TryXFromPath(segments, out canonicalUrl),
            MemberSocialChannel.Instagram => TryInstagramFromPath(segments, out canonicalUrl),
            MemberSocialChannel.Facebook => TryFacebookFromUri(segments, uri, out canonicalUrl),
            MemberSocialChannel.YouTube => TryYouTubeFromUri(host, segments, out canonicalUrl),
            MemberSocialChannel.TikTok => TryTikTokFromPath(segments, out canonicalUrl),
            MemberSocialChannel.Bluesky => TryBlueskyFromUri(host, segments, out canonicalUrl),
            _ => false,
        };
    }

    private static bool TryXFromPath(IReadOnlyList<string> segments, [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (segments.Count != 1 || XReservedPath().IsMatch(segments[0]))
        {
            return false;
        }

        var handle = StripLeadingAt(segments[0]);
        if (!XHandle().IsMatch(handle))
        {
            return false;
        }

        canonicalUrl = $"https://x.com/{handle}";
        return true;
    }

    private static bool TryInstagramFromPath(IReadOnlyList<string> segments, [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (segments.Count != 1 || InstagramReservedPath().IsMatch(segments[0]))
        {
            return false;
        }

        var handle = StripLeadingAt(segments[0]);
        if (!InstagramHandle().IsMatch(handle))
        {
            return false;
        }

        canonicalUrl = $"https://www.instagram.com/{handle}";
        return true;
    }

    private static bool TryFacebookFromUri(
        IReadOnlyList<string> segments,
        Uri uri,
        [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (segments.Count == 1
            && segments[0].Equals("profile.php", StringComparison.OrdinalIgnoreCase))
        {
            var id = GetQueryValue(uri, "id");
            if (string.IsNullOrWhiteSpace(id) || !FacebookProfileId().IsMatch(id))
            {
                return false;
            }

            canonicalUrl = $"https://www.facebook.com/profile.php?id={id}";
            return true;
        }

        if (segments.Count == 1)
        {
            if (FacebookReservedPath().IsMatch(segments[0]) || !FacebookPathSegment().IsMatch(segments[0]))
            {
                return false;
            }

            canonicalUrl = $"https://www.facebook.com/{segments[0]}";
            return true;
        }

        if (segments.Count == 3
            && (segments[0].Equals("pages", StringComparison.OrdinalIgnoreCase)
                || segments[0].Equals("people", StringComparison.OrdinalIgnoreCase))
            && FacebookPathSegment().IsMatch(segments[1])
            && FacebookProfileId().IsMatch(segments[2]))
        {
            canonicalUrl = $"https://www.facebook.com/{segments[0].ToLowerInvariant()}/{segments[1]}/{segments[2]}";
            return true;
        }

        return false;
    }

    private static bool TryYouTubeFromUri(
        string host,
        IReadOnlyList<string> segments,
        [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (host is "youtu.be")
        {
            return false;
        }

        if (segments.Count == 1 && segments[0].StartsWith('@'))
        {
            var handle = StripLeadingAt(segments[0]);
            if (!YouTubeHandle().IsMatch(handle))
            {
                return false;
            }

            canonicalUrl = $"https://www.youtube.com/@{handle}";
            return true;
        }

        if (segments.Count == 2 && segments[0].Equals("channel", StringComparison.OrdinalIgnoreCase)
            && YouTubeChannelId().IsMatch(segments[1]))
        {
            canonicalUrl = $"https://www.youtube.com/channel/{segments[1]}";
            return true;
        }

        if (segments.Count == 2 && segments[0].Equals("c", StringComparison.OrdinalIgnoreCase)
            && YouTubeCustomName().IsMatch(segments[1]))
        {
            canonicalUrl = $"https://www.youtube.com/c/{segments[1]}";
            return true;
        }

        if (segments.Count == 2 && segments[0].Equals("user", StringComparison.OrdinalIgnoreCase)
            && YouTubeCustomName().IsMatch(segments[1]))
        {
            canonicalUrl = $"https://www.youtube.com/user/{segments[1]}";
            return true;
        }

        return false;
    }

    private static bool TryTikTokFromPath(IReadOnlyList<string> segments, [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (segments.Count != 1)
        {
            return false;
        }

        var handle = StripLeadingAt(segments[0]);
        if (TikTokReservedPath().IsMatch(handle) || !TikTokHandle().IsMatch(handle))
        {
            return false;
        }

        canonicalUrl = $"https://www.tiktok.com/@{handle}";
        return true;
    }

    private static bool TryBlueskyFromUri(
        string host,
        IReadOnlyList<string> segments,
        [NotNullWhen(true)] out string? canonicalUrl)
    {
        canonicalUrl = null;
        if (host.EndsWith(".bsky.social", StringComparison.Ordinal))
        {
            if (segments.Count > 0 || !BlueskyHandle().IsMatch(host))
            {
                return false;
            }

            canonicalUrl = $"https://bsky.app/profile/{host}";
            return true;
        }

        if (segments.Count != 2 || !segments[0].Equals("profile", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var handle = StripLeadingAt(segments[1]).ToLowerInvariant();
        if (!BlueskyHandle().IsMatch(handle))
        {
            return false;
        }

        canonicalUrl = $"https://bsky.app/profile/{handle}";
        return true;
    }

    private static bool IsValidHandle(MemberSocialChannel channel, string handle) => channel switch
    {
        MemberSocialChannel.X => XHandle().IsMatch(handle),
        MemberSocialChannel.Instagram => InstagramHandle().IsMatch(handle),
        MemberSocialChannel.Facebook => FacebookHandle().IsMatch(handle),
        MemberSocialChannel.YouTube => YouTubeHandle().IsMatch(handle),
        MemberSocialChannel.TikTok => TikTokHandle().IsMatch(handle),
        MemberSocialChannel.Bluesky => BlueskyHandle().IsMatch(NormalizeBlueskyHandle(handle)),
        _ => false,
    };

    private static string BuildFromHandle(MemberSocialChannel channel, string handle) => channel switch
    {
        MemberSocialChannel.X => $"https://x.com/{handle}",
        MemberSocialChannel.Instagram => $"https://www.instagram.com/{handle}",
        MemberSocialChannel.Facebook => $"https://www.facebook.com/{handle}",
        MemberSocialChannel.YouTube => $"https://www.youtube.com/@{handle}",
        MemberSocialChannel.TikTok => $"https://www.tiktok.com/@{handle}",
        MemberSocialChannel.Bluesky => $"https://bsky.app/profile/{NormalizeBlueskyHandle(handle)}",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    private static string NormalizeBlueskyHandle(string handle)
    {
        var trimmed = handle.Trim().ToLowerInvariant();
        return trimmed.Contains('.', StringComparison.Ordinal) ? trimmed : $"{trimmed}.bsky.social";
    }

    private static bool HostIsAllowed(MemberSocialChannel channel, string host) => channel switch
    {
        MemberSocialChannel.X => host is "x.com" or "twitter.com",
        MemberSocialChannel.Instagram => host is "instagram.com",
        MemberSocialChannel.Facebook => host is "facebook.com" or "fb.com" or "m.facebook.com",
        MemberSocialChannel.YouTube => host is "youtube.com" or "youtu.be" or "m.youtube.com",
        MemberSocialChannel.TikTok => host is "tiktok.com",
        MemberSocialChannel.Bluesky => host is "bsky.app" || host.EndsWith(".bsky.social", StringComparison.Ordinal),
        _ => false,
    };

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }

    private static IReadOnlyList<string> GetPathSegments(Uri uri)
    {
        var path = Uri.UnescapeDataString(uri.AbsolutePath).Trim('/');
        if (string.IsNullOrEmpty(path))
        {
            return [];
        }

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string StripLeadingAt(string value) =>
        value.StartsWith('@') ? value[1..] : value;

    private static string? GetQueryValue(Uri uri, string name)
    {
        var query = uri.Query.TrimStart('?');
        if (query.Length == 0)
        {
            return null;
        }

        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var key = Uri.UnescapeDataString(separator < 0 ? part : part[..separator]);
            if (!key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return separator < 0 ? string.Empty : Uri.UnescapeDataString(part[(separator + 1)..].Replace('+', ' '));
        }

        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_]{1,15}$")]
    private static partial Regex XHandle();

    [GeneratedRegex("^(?i)(home|explore|search|settings|i|intent|compose|messages|notifications|login|signup|share|tos|privacy|about|download|help|jobs|ads)$")]
    private static partial Regex XReservedPath();

    [GeneratedRegex("^[A-Za-z0-9._]{1,30}$")]
    private static partial Regex InstagramHandle();

    [GeneratedRegex("^(?i)(p|reel|reels|stories|explore|accounts|direct|legal|about|developer|directory)$")]
    private static partial Regex InstagramReservedPath();

    [GeneratedRegex("^[A-Za-z0-9.]{1,50}$")]
    private static partial Regex FacebookHandle();

    [GeneratedRegex("^[A-Za-z0-9._-]{1,80}$")]
    private static partial Regex FacebookPathSegment();

    [GeneratedRegex("^[0-9]{1,20}$")]
    private static partial Regex FacebookProfileId();

    [GeneratedRegex("^(?i)(sharer|share|watch|login|dialog|tr|ads|privacy|help|policies|bookmark|photo\\.php|story\\.php|groups)$")]
    private static partial Regex FacebookReservedPath();

    [GeneratedRegex("^[A-Za-z0-9._-]{3,30}$")]
    private static partial Regex YouTubeHandle();

    [GeneratedRegex("^UC[A-Za-z0-9_-]{20,24}$")]
    private static partial Regex YouTubeChannelId();

    [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$")]
    private static partial Regex YouTubeCustomName();

    [GeneratedRegex("^[A-Za-z0-9._]{2,24}$")]
    private static partial Regex TikTokHandle();

    [GeneratedRegex("^(?i)(fyp|following|search|live|music|tag|video|t|discover|explore)$")]
    private static partial Regex TikTokReservedPath();

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?(\\.[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?)+$")]
    private static partial Regex BlueskyHandle();
}
