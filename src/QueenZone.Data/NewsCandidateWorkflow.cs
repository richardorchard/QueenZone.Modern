namespace QueenZone.Data;

/// <summary>
/// Single source of truth for discovery candidate status transitions.
/// Admin handlers, repositories, and news-agent worker services should use these helpers
/// instead of encoding transition rules inline.
/// </summary>
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

    public static bool IsTerminal(NewsCandidateStatus status) =>
        status is NewsCandidateStatus.Rejected
            or NewsCandidateStatus.IgnoredDuplicate
            or NewsCandidateStatus.PromotedToArticle;

    public static bool CanTransition(NewsCandidateStatus current, NewsCandidateStatus next) =>
        AllowedTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);

    public static IReadOnlyList<NewsCandidateStatus> GetAllowedTransitions(NewsCandidateStatus current) =>
        AllowedTransitions.TryGetValue(current, out var allowed)
            ? allowed
            : [];

    /// <summary>
    /// Validates a status change for persistence. Same-status updates are allowed so
    /// repositories can refresh notes/scores without changing status.
    /// </summary>
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

        error = FormatInvalidTransitionError(current, next);
        return false;
    }

    /// <summary>
    /// Validates a deliberate user/worker action that must change status.
    /// Already being in the target status is a failure with a clear message.
    /// </summary>
    public static bool TryValidateStatusChange(
        NewsCandidateStatus current,
        NewsCandidateStatus next,
        out string? error)
    {
        if (current == next)
        {
            error = FormatAlreadyAtStatusError(next);
            return false;
        }

        if (CanTransition(current, next))
        {
            error = null;
            return true;
        }

        error = FormatInvalidTransitionError(current, next);
        return false;
    }

    public static bool CanReject(NewsCandidateStatus status) =>
        CanTransition(status, NewsCandidateStatus.Rejected);

    public static bool CanIgnoreAsDuplicate(NewsCandidateStatus status) =>
        CanTransition(status, NewsCandidateStatus.IgnoredDuplicate);

    public static bool CanMarkDrafted(NewsCandidateStatus status) =>
        CanTransition(status, NewsCandidateStatus.Drafted);

    public static bool CanPromoteToArticle(NewsCandidateStatus status) =>
        CanTransition(status, NewsCandidateStatus.PromotedToArticle);

    /// <summary>
    /// Draft generation/regeneration is allowed while the candidate is awaiting or already drafted.
    /// </summary>
    public static bool CanGenerateOrRegenerateDraft(NewsCandidateStatus status) =>
        status is NewsCandidateStatus.NeedsReview or NewsCandidateStatus.Drafted;

    /// <summary>
    /// Automated triage only processes newly discovered candidates.
    /// </summary>
    public static bool CanTriage(NewsCandidateStatus status) =>
        status == NewsCandidateStatus.Discovered;

    /// <summary>
    /// Promotion requires Drafted. NeedsReview may be auto-drafted first when a draft payload exists.
    /// </summary>
    public static bool CanPrepareForPromotion(NewsCandidateStatus status) =>
        status is NewsCandidateStatus.Drafted or NewsCandidateStatus.NeedsReview;

    public static string FormatAlreadyAtStatusError(NewsCandidateStatus status) =>
        status switch
        {
            NewsCandidateStatus.Rejected => "This candidate has already been rejected.",
            NewsCandidateStatus.IgnoredDuplicate => "This candidate has already been ignored as a duplicate.",
            NewsCandidateStatus.PromotedToArticle => "This candidate has already been promoted to an article.",
            NewsCandidateStatus.Drafted => "This candidate is already drafted.",
            NewsCandidateStatus.NeedsReview => "This candidate is already in needs-review.",
            NewsCandidateStatus.Discovered => "This candidate is already discovered.",
            _ => $"This candidate is already in status {status}."
        };

    public static string FormatInvalidTransitionError(NewsCandidateStatus current, NewsCandidateStatus next) =>
        $"Cannot transition candidate status from {current} to {next}.";

    public static string GetRejectError(NewsCandidateStatus status) =>
        TryValidateStatusChange(status, NewsCandidateStatus.Rejected, out var error)
            ? string.Empty
            : error ?? FormatInvalidTransitionError(status, NewsCandidateStatus.Rejected);

    public static string GetIgnoreDuplicateError(NewsCandidateStatus status) =>
        TryValidateStatusChange(status, NewsCandidateStatus.IgnoredDuplicate, out var error)
            ? string.Empty
            : error ?? FormatInvalidTransitionError(status, NewsCandidateStatus.IgnoredDuplicate);

    public static string GetDraftGenerationError(NewsCandidateStatus status) =>
        CanGenerateOrRegenerateDraft(status)
            ? string.Empty
            : "Only needs-review or drafted candidates can regenerate a draft.";

    public static string GetTriageError(NewsCandidateStatus status) =>
        CanTriage(status)
            ? string.Empty
            : $"Candidate is not in the discovered state (current status: {status}).";

    public static string GetPromoteReadinessError(NewsCandidateStatus status) =>
        status switch
        {
            NewsCandidateStatus.Drafted => string.Empty,
            NewsCandidateStatus.NeedsReview => string.Empty,
            _ => $"Only drafted candidates can be promoted. Current status: {status}."
        };

    public static string GetMarkDraftedError(NewsCandidateStatus status) =>
        TryValidateStatusChange(status, NewsCandidateStatus.Drafted, out var error)
            ? string.Empty
            : error ?? FormatInvalidTransitionError(status, NewsCandidateStatus.Drafted);
}
