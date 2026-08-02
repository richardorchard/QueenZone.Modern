using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class PrivateMessageEntity
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }

    public Guid SenderMemberId { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Database-assigned insert order used as the stable unread cursor.
    /// Prefer this over <see cref="CreatedAt"/>, which is not unique or commit-ordered.
    /// </summary>
    public long SortKey { get; set; }

    public PrivateConversationEntity? Conversation { get; set; }

    public MemberAccount? Sender { get; set; }
}
