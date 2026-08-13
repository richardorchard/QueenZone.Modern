namespace QueenZone.Data.Entities;

public sealed class MemberAccountDeletionAuditLogEntity
{
    public long Id { get; set; }

    public Guid MemberAccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}
