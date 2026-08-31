namespace QueenZone.Data;

/// <summary>
/// Thrown when an admin or member write loses a compare-and-swap against a newer row.
/// Callers should reload current values and ask the user to review and resubmit.
/// </summary>
public sealed class OptimisticConcurrencyException : InvalidOperationException
{
    public const string UserMessage =
        "This was changed by someone else. Review the current values and resubmit.";

    public OptimisticConcurrencyException()
        : base(UserMessage)
    {
    }

    public OptimisticConcurrencyException(string message)
        : base(message)
    {
    }
}
