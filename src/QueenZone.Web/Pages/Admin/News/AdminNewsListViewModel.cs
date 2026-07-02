using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class AdminNewsListViewModel
{
    public required IReadOnlyList<AdminNewsArticle> Articles { get; init; }

    public required string AntiforgeryToken { get; init; }

    public string? StatusMessage { get; init; }

    public string? StatusMessageKind { get; init; }

    public int TotalCount { get; init; }

    public int RangeStart { get; init; }

    public int RangeEnd { get; init; }

    public ArchivePaginationViewModel? Pagination { get; init; }

    public static AdminNewsListViewModel FromPageModel(AdminNewsListPageModel page, string antiforgeryToken) =>
        new()
        {
            Articles = page.Articles,
            AntiforgeryToken = antiforgeryToken,
            StatusMessage = page.StatusMessage,
            StatusMessageKind = page.StatusMessageKind,
            TotalCount = page.TotalCount,
            RangeStart = page.RangeStart,
            RangeEnd = page.RangeEnd,
            Pagination = page.Pagination,
        };
}
