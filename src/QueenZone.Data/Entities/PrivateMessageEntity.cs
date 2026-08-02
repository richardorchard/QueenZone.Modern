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

    public PrivateConversationEntity? Conversation { get; set; }

    public MemberAccount? Sender { get; set; }
}
