namespace QueenZone.Web.Archive;

public sealed record ArchivePageContext(
    int CurrentPage,
    int TotalPages,
    string Title,
    string CanonicalPath,
    string? PrevPath,
    string? NextPath);
