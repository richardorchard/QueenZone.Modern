namespace QueenZone.Data;

/// <summary>
/// Allowed status transitions for member fan-performance submissions.
/// Photo-shaped, plus member withdraw to <see cref="FanPerformanceSubmissionStatus.Withdrawn"/>.
/// </summary>
public static class FanPerformanceSubmissionWorkflow
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [FanPerformanceSubmissionStatus.Pending] =
            [
                FanPerformanceSubmissionStatus.UnderReview,
                FanPerformanceSubmissionStatus.NeedsInfo,
                FanPerformanceSubmissionStatus.Approved,
                FanPerformanceSubmissionStatus.Rejected,
                FanPerformanceSubmissionStatus.Withdrawn,
            ],
            [FanPerformanceSubmissionStatus.UnderReview] =
            [
                FanPerformanceSubmissionStatus.NeedsInfo,
                FanPerformanceSubmissionStatus.Approved,
                FanPerformanceSubmissionStatus.Rejected,
                FanPerformanceSubmissionStatus.Withdrawn,
            ],
            [FanPerformanceSubmissionStatus.NeedsInfo] =
            [
                FanPerformanceSubmissionStatus.UnderReview,
                FanPerformanceSubmissionStatus.Approved,
                FanPerformanceSubmissionStatus.Rejected,
                FanPerformanceSubmissionStatus.Withdrawn,
            ],
            [FanPerformanceSubmissionStatus.Approved] = [],
            [FanPerformanceSubmissionStatus.Rejected] = [],
            [FanPerformanceSubmissionStatus.Withdrawn] = [],
        };

    public static bool IsTerminal(string status) =>
        string.Equals(status, FanPerformanceSubmissionStatus.Approved, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, FanPerformanceSubmissionStatus.Rejected, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, FanPerformanceSubmissionStatus.Withdrawn, StringComparison.OrdinalIgnoreCase);

    public static bool CanMemberWithdraw(string status) =>
        CanTransition(status, FanPerformanceSubmissionStatus.Withdrawn);

    public static bool CanMemberReplyNeedsInfo(string status) =>
        FanPerformanceSubmissionStatus.IsKnown(status)
        && string.Equals(
            FanPerformanceSubmissionStatus.Normalize(status),
            FanPerformanceSubmissionStatus.NeedsInfo,
            StringComparison.Ordinal);

    public static bool CanTransition(string current, string next)
    {
        if (!FanPerformanceSubmissionStatus.IsKnown(current) || !FanPerformanceSubmissionStatus.IsKnown(next))
        {
            return false;
        }

        var normalizedCurrent = FanPerformanceSubmissionStatus.Normalize(current);
        var normalizedNext = FanPerformanceSubmissionStatus.Normalize(next);
        return AllowedTransitions.TryGetValue(normalizedCurrent, out var allowed)
            && allowed.Contains(normalizedNext, StringComparer.Ordinal);
    }

    public static bool TryValidateStatusChange(string current, string next, out string? error)
    {
        if (!FanPerformanceSubmissionStatus.IsKnown(current))
        {
            error = $"Unknown current status '{current}'.";
            return false;
        }

        if (!FanPerformanceSubmissionStatus.IsKnown(next))
        {
            error = $"Unknown target status '{next}'.";
            return false;
        }

        var normalizedCurrent = FanPerformanceSubmissionStatus.Normalize(current);
        var normalizedNext = FanPerformanceSubmissionStatus.Normalize(next);

        if (string.Equals(normalizedCurrent, normalizedNext, StringComparison.Ordinal))
        {
            error = $"This submission is already {normalizedNext}.";
            return false;
        }

        if (CanTransition(normalizedCurrent, normalizedNext))
        {
            error = null;
            return true;
        }

        error = $"Cannot transition fan-performance submission status from {normalizedCurrent} to {normalizedNext}.";
        return false;
    }
}
