namespace QueenZone.Data.Entities;

public sealed class MemberAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Blob path within the ugc-avatars container (not a full URL), e.g.
    /// <c>members/{guid}/avatar-{id}.webp</c>. Null when the member has no custom avatar.
    /// </summary>
    public string? AvatarUrl { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int? LinkedLegacyUserId { get; set; }
}
