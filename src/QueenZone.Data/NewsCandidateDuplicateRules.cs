namespace QueenZone.Data;

public static class NewsCandidateDuplicateRules
{
    public static bool IsActiveDuplicateSource(NewsCandidateStatus status) =>
        status is not NewsCandidateStatus.Rejected and not NewsCandidateStatus.IgnoredDuplicate;
}
