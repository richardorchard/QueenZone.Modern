using QueenZone.Data.Entities;

namespace QueenZone.Data;

public static class MemberAccountDeletionPolicy
{
    public const string DeletedDisplayName = "Deleted member";

    public const int RetentionDays = 30;

    public const string RequestedAuditAction = "Requested";

    public const string PurgedAuditAction = "PersonalDataPurged";

    public static string CreateDeletedEmail(Guid memberId) =>
        $"deleted-{memberId:N}@deleted.invalid";
}

public sealed record MemberAccountDeletionRequestResult(
    MemberAccount Account,
    string? PreviousAvatarUrl,
    bool AlreadyRequested);
