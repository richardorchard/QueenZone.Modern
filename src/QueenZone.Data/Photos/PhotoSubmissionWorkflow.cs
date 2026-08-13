namespace QueenZone.Data;

/// <summary>
/// Allowed status transitions for member photo submissions.
/// </summary>
public static class PhotoSubmissionWorkflow
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [PhotoSubmissionStatus.Pending] =
            [
                PhotoSubmissionStatus.UnderReview,
                PhotoSubmissionStatus.NeedsInfo,
                PhotoSubmissionStatus.Approved,
                PhotoSubmissionStatus.Rejected,
            ],
            [PhotoSubmissionStatus.UnderReview] =
            [
                PhotoSubmissionStatus.NeedsInfo,
                PhotoSubmissionStatus.Approved,
                PhotoSubmissionStatus.Rejected,
            ],
            [PhotoSubmissionStatus.NeedsInfo] =
            [
                PhotoSubmissionStatus.UnderReview,
                PhotoSubmissionStatus.Approved,
                PhotoSubmissionStatus.Rejected,
            ],
            [PhotoSubmissionStatus.Approved] = [],
            [PhotoSubmissionStatus.Rejected] = [],
        };

    public static bool IsTerminal(string status) =>
        string.Equals(status, PhotoSubmissionStatus.Approved, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, PhotoSubmissionStatus.Rejected, StringComparison.OrdinalIgnoreCase);

    public static bool CanTransition(string current, string next)
    {
        if (!PhotoSubmissionStatus.IsKnown(current) || !PhotoSubmissionStatus.IsKnown(next))
        {
            return false;
        }

        var normalizedCurrent = PhotoSubmissionStatus.Normalize(current);
        var normalizedNext = PhotoSubmissionStatus.Normalize(next);
        return AllowedTransitions.TryGetValue(normalizedCurrent, out var allowed)
            && allowed.Contains(normalizedNext, StringComparer.Ordinal);
    }

    public static bool TryValidateStatusChange(string current, string next, out string? error)
    {
        if (!PhotoSubmissionStatus.IsKnown(current))
        {
            error = $"Unknown current status '{current}'.";
            return false;
        }

        if (!PhotoSubmissionStatus.IsKnown(next))
        {
            error = $"Unknown target status '{next}'.";
            return false;
        }

        var normalizedCurrent = PhotoSubmissionStatus.Normalize(current);
        var normalizedNext = PhotoSubmissionStatus.Normalize(next);

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

        error = $"Cannot transition photo submission status from {normalizedCurrent} to {normalizedNext}.";
        return false;
    }
}
