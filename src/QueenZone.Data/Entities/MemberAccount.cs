namespace QueenZone.Data.Entities;

public sealed class MemberAccount
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? LinkedLegacyUserId { get; set; }
}
