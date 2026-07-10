namespace QueenZone.Web;

/// <summary>
/// Input for the shared member avatar partial / tag helper.
/// When <see cref="MemberId"/> and <see cref="HasAvatar"/> are set, the proxy image URL is used;
/// otherwise an initials fallback is rendered.
/// </summary>
public sealed class MemberAvatarViewModel
{
    public required string DisplayName { get; init; }

    public Guid? MemberId { get; init; }

    public bool HasAvatar { get; init; }

    /// <summary>
    /// When true, requests the 64px thumbnail via the avatar proxy.
    /// </summary>
    public bool UseThumbnail { get; init; }

    /// <summary>
    /// CSS size variant: sm (header/lists) or md (settings).
    /// </summary>
    public string Size { get; init; } = "sm";

    public string AltText => $"{DisplayName}'s avatar";

    public string Initials
    {
        get
        {
            var trimmed = DisplayName.Trim();
            if (trimmed.Length == 0)
            {
                return "?";
            }

            return char.ToUpperInvariant(trimmed[0]).ToString();
        }
    }

    public string? ImageUrl =>
        MemberId is Guid id && HasAvatar
            ? MemberAvatarPaths.GetServePath(id, UseThumbnail)
            : null;

    /// <summary>
    /// Stable hue for the initials circle based on the display name.
    /// </summary>
    public string InitialsBackgroundCss
    {
        get
        {
            var hash = 0;
            foreach (var ch in DisplayName)
            {
                hash = (hash * 31) + ch;
            }

            var hue = Math.Abs(hash) % 360;
            return $"hsl({hue} 42% 38%)";
        }
    }
}
