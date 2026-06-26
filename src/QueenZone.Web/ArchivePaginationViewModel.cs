namespace QueenZone.Web;

/// <summary>
/// Data for the shared <c>_ArchivePagination</c> partial. <see cref="Pages"/> entries
/// represent either an ellipsis (<see cref="ArchivePaginationPageLink.PageNumber"/> is
/// <see langword="null"/>), the current page (<see cref="ArchivePaginationPageLink.IsCurrent"/>
/// is <see langword="true"/>, no <see cref="ArchivePaginationPageLink.Href"/>), or a linked page.
/// </summary>
public sealed class ArchivePaginationViewModel
{
    public required string AriaLabel { get; init; }

    public required int CurrentPage { get; init; }

    public required int TotalPages { get; init; }

    public string? PreviousHref { get; init; }

    public string? NextHref { get; init; }

    public required IReadOnlyList<ArchivePaginationPageLink> Pages { get; init; }
}

public sealed class ArchivePaginationPageLink
{
    public int? PageNumber { get; init; }

    public string? Href { get; init; }

    public bool IsCurrent { get; init; }
}
