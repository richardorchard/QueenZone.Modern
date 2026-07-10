namespace QueenZone.Web;

/// <summary>
/// Blob path conventions for member avatars in the ugc-avatars container.
/// AvatarUrl on MemberAccount stores the full-size blob path (not a URL).
/// </summary>
public static class MemberAvatarPaths
{
    public const string Container = QueenZone.Storage.BlobUploadContainers.Avatars;

    public const int FullSizePixels = 256;

    public const int ThumbSizePixels = 64;

    public const long MaxUploadBytes = 2 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    public static string CreateAvatarBlobName(Guid memberId)
    {
        var id = Guid.NewGuid().ToString("N");
        return $"members/{memberId:N}/avatar-{id}.webp";
    }

    public static string ToThumbBlobName(string avatarBlobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(avatarBlobName);
        const string suffix = ".webp";
        if (avatarBlobName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return avatarBlobName[..^suffix.Length] + "-thumb.webp";
        }

        return avatarBlobName + "-thumb";
    }

    public static string GetServePath(Guid memberId, bool thumb = false) =>
        thumb
            ? $"/account/avatar/{memberId:D}?size=thumb"
            : $"/account/avatar/{memberId:D}";
}
