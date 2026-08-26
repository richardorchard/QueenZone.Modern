namespace QueenZone.Data;

public sealed class DuplicateActiveNewsSuggestionException : Exception
{
    public DuplicateActiveNewsSuggestionException()
        : this(null)
    {
    }

    public DuplicateActiveNewsSuggestionException(Exception? innerException)
        : base("An active news suggestion already exists for this URL.", innerException)
    {
    }
}
