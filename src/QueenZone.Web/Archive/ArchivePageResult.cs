namespace QueenZone.Web.Archive;

public abstract class ArchivePageResult<T>
{
    private ArchivePageResult() { }

    public sealed class Success : ArchivePageResult<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public required ArchivePageContext Context { get; init; }
    }

    public sealed class NotFound : ArchivePageResult<T> { }
}
