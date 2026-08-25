namespace QueenZone.Data;

/// <summary>
/// Known <see cref="Entities.PrivateMessageReportAuditLogEntity.Action"/> values.
/// Written by the moderator review surface (issue #470) per ADR 0015.
/// </summary>
public static class PrivateMessageReportAuditAction
{
    /// <summary>A moderator opened the report's snapshotted message content.</summary>
    public const string Viewed = "Viewed";

    /// <summary>A moderator changed the report's status. <c>Details</c> records old and new status.</summary>
    public const string StatusChanged = "StatusChanged";
}
