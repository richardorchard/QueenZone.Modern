using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Per-participant state for a private conversation (read cursor, archive flag).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PrivateConversationParticipantEntity
{
    public Guid ConversationId { get; set; }

    public Guid MemberId { get; set; }

    public DateTimeOffset? LastReadAt { get; set; }

    /// <summary>
    /// Highest <see cref="PrivateMessageEntity.SortKey"/> observed by this participant.
    /// Null means the participant has never opened the conversation.
    /// </summary>
    public long? LastReadSortKey { get; set; }

    /// <summary>
    /// When true, the conversation is hidden from this member's inbox (phase 2).
    /// MVP unread counts exclude archived conversations.
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    /// When true, this member has removed the conversation from their own account. Hidden from
    /// both the inbox and the archive view. A new message from either participant clears this
    /// (and <see cref="IsArchived"/>) for both participants, restoring visibility rather than
    /// silently dropping the message.
    /// </summary>
    public bool IsRemoved { get; set; }

    public PrivateConversationEntity? Conversation { get; set; }

    public MemberAccount? Member { get; set; }
}
