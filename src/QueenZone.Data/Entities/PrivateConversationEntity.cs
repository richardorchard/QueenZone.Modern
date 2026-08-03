using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// One-to-one private conversation between two members.
/// <see cref="MemberLowId"/> / <see cref="MemberHighId"/> store the ordered pair for a unique index.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PrivateConversationEntity
{
    public Guid Id { get; set; }

    public Guid MemberLowId { get; set; }

    public Guid MemberHighId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastMessageAt { get; set; }

    /// <summary>
    /// Tip message <see cref="PrivateMessageEntity.SortKey"/> used for inbox ranking.
    /// Prefer this over <see cref="LastMessageAt"/>, which stays monotonic for display but can
    /// disagree with commit order under clock skew.
    /// </summary>
    public long LastMessageSortKey { get; set; }

    public string LastMessagePreview { get; set; } = string.Empty;

    public Guid? LastMessageSenderId { get; set; }

    public MemberAccount? MemberLow { get; set; }

    public MemberAccount? MemberHigh { get; set; }

    public ICollection<PrivateConversationParticipantEntity> Participants { get; set; } =
        new List<PrivateConversationParticipantEntity>();

    public ICollection<PrivateMessageEntity> Messages { get; set; } = new List<PrivateMessageEntity>();
}
