using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

public enum NotificationCategory
{
    ForumReply,
    PrivateMessage,
    News,
}

[ExcludeFromCodeCoverage]
public sealed class NotificationPreferenceEntity
{
    public Guid MemberAccountId { get; set; }

    public NotificationCategory Category { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime UpdatedAt { get; set; }

    public MemberAccount? MemberAccount { get; set; }
}
