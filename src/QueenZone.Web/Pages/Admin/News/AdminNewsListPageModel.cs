using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public abstract class AdminNewsListPageModel(IAdminNewsRepository adminNewsRepository) : AdminNewsPageModel
{
    protected IAdminNewsRepository AdminNewsRepository => adminNewsRepository;
    public IReadOnlyList<AdminNewsArticle> Articles { get; private set; } = [];

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public int CurrentPage { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }

    public int RangeStart { get; private set; }

    public int RangeEnd { get; private set; }

    public ArchivePaginationViewModel? Pagination { get; private set; }

    protected async Task<IActionResult> LoadListPageAsync(int page, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return Redirect(AdminNewsRoutes.GetListPath(1));
        }

        var result = await adminNewsRepository.GetPageAsync(page, AdminNewsRoutes.ListPageSize, cancellationToken);
        TotalCount = result.TotalCount;
        TotalPages = AdminNewsRoutes.GetListTotalPages(TotalCount);
        CurrentPage = page;

        if (TotalPages > 0 && page > TotalPages)
        {
            return Redirect(AdminNewsRoutes.GetListPath(TotalPages));
        }

        Articles = result.Items;
        RangeStart = TotalCount == 0 ? 0 : ((CurrentPage - 1) * AdminNewsRoutes.ListPageSize) + 1;
        RangeEnd = TotalCount == 0 ? 0 : RangeStart + Articles.Count - 1;
        Pagination = AdminNewsRoutes.GetListPaginationViewModel(CurrentPage, TotalPages);
        StatusMessage = TempData[AdminNewsMessages.MessageKey] as string;
        StatusMessageKind = TempData[AdminNewsMessages.MessageKindKey] as string;
        ViewData["Title"] = CurrentPage <= 1 ? "Admin news" : $"Admin news – Page {CurrentPage}";
        return Page();
    }
}
