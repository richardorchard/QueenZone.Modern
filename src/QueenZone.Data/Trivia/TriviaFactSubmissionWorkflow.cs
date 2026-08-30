namespace QueenZone.Data;

/// <summary>
/// Allowed status transitions for member trivia fact suggestions.
/// </summary>
public static class TriviaFactSubmissionWorkflow
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [TriviaFactSubmissionStatus.Pending] =
            [
                TriviaFactSubmissionStatus.Approved,
                TriviaFactSubmissionStatus.Rejected,
            ],
            [TriviaFactSubmissionStatus.Approved] = [],
            [TriviaFactSubmissionStatus.Rejected] = [],
        };

    public static bool IsTerminal(string status) =>
        string.Equals(status, TriviaFactSubmissionStatus.Approved, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, TriviaFactSubmissionStatus.Rejected, StringComparison.OrdinalIgnoreCase);

    public static bool CanTransition(string current, string next)
    {
        if (!TriviaFactSubmissionStatus.IsKnown(current) || !TriviaFactSubmissionStatus.IsKnown(next))
        {
            return false;
        }

        var normalizedCurrent = TriviaFactSubmissionStatus.Normalize(current);
        var normalizedNext = TriviaFactSubmissionStatus.Normalize(next);
        return AllowedTransitions.TryGetValue(normalizedCurrent, out var allowed)
            && allowed.Contains(normalizedNext, StringComparer.Ordinal);
    }

    public static bool TryValidateStatusChange(string current, string next, out string? error)
    {
        if (!TriviaFactSubmissionStatus.IsKnown(current))
        {
            error = $"Unknown current status '{current}'.";
            return false;
        }

        if (!TriviaFactSubmissionStatus.IsKnown(next))
        {
            error = $"Unknown target status '{next}'.";
            return false;
        }

        var normalizedCurrent = TriviaFactSubmissionStatus.Normalize(current);
        var normalizedNext = TriviaFactSubmissionStatus.Normalize(next);

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

        error = $"Cannot transition trivia fact submission status from {normalizedCurrent} to {normalizedNext}.";
        return false;
    }
}
