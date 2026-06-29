namespace QueenZone.Data.Entities;

public sealed class MemberExternalLogin
{
    public Guid Id { get; set; }

    public Guid MemberAccountId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderKey { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime LinkedAt { get; set; }
}
