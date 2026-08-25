using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Member-submitted report of an abusive private message. Snapshots enough of the
/// reported message (and a little preceding context) so moderators can review it
/// without a query path over the rest of private conversations.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PrivateMessageReportEntity
{
    public Guid Id { get; set; }

    public Guid MessageId { get; set; }

    public Guid ConversationId { get; set; }

    public Guid ReporterMemberId { get; set; }

    public Guid ReportedMemberId { get; set; }

    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string Status { get; set; } = PrivateMessageReportStatus.Open;

    public string MessageBodySnapshot { get; set; } = string.Empty;

    public string SenderDisplayNameSnapshot { get; set; } = string.Empty;

    public DateTimeOffset MessageCreatedAtSnapshot { get; set; }

    public long MessageSortKeySnapshot { get; set; }

    /// <summary>
    /// JSON array of up to <see cref="PrivateMessageLimits.ReportPrecedingMessageCount"/>
    /// messages immediately before the reported one, captured at report time.
    /// </summary>
    public string? PrecedingContextJson { get; set; }

    public PrivateMessageEntity? Message { get; set; }

    public PrivateConversationEntity? Conversation { get; set; }

    public MemberAccount? Reporter { get; set; }

    public MemberAccount? Reported { get; set; }
}
