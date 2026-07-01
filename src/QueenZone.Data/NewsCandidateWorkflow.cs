namespace QueenZone.Data;

public static class NewsCandidateWorkflow
{
    private static readonly IReadOnlyDictionary<NewsCandidateStatus, NewsCandidateStatus[]> AllowedTransitions =
        new Dictionary<NewsCandidateStatus, NewsCandidateStatus[]>
        {
            [NewsCandidateStatus.Discovered] =
            [
                NewsCandidateStatus.NeedsReview,
                NewsCandidateStatus.Rejected,
                NewsCandidateStatus.IgnoredDuplicate
            ],
            [NewsCandidateStatus.NeedsReview] =
            [
                NewsCandidateStatus.Drafted,
                NewsCandidateStatus.Rejected,
                NewsCandidateStatus.IgnoredDuplicate
            ],
            [NewsCandidateStatus.Drafted] =
            [
                NewsCandidateStatus.PromotedToArticle,
                NewsCandidateStatus.NeedsReview,
                NewsCandidateStatus.Rejected,
                NewsCandidateStatus.IgnoredDuplicate
            ],
            [NewsCandidateStatus.Rejected] = [],
            [NewsCandidateStatus.IgnoredDuplicate] = [],
            [NewsCandidateStatus.PromotedToArticle] = []
        };

    public static bool CanTransition(NewsCandidateStatus current, NewsCandidateStatus next) =>
        AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);

    public static bool TryTransition(NewsCandidateStatus current, NewsCandidateStatus next, out string? error)
    {
        if (current == next)
        {
            error = null;
            return true;
        }

        if (CanTransition(current, next))
        {
            error = null;
            return true;
        }

        error = $"Cannot transition candidate status from {current} to {next}.";
        return false;
    }
}
