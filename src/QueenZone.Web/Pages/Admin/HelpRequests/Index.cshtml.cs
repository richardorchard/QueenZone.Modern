using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.HelpRequests;

public sealed class IndexModel(IHelpRequestRepository helpRequestRepository) : AdminHelpRequestsPageModel
{
    public HelpRequestListPage List { get; private set; } =
        new([], 0, HelpRequestStatus.Open);

    public int PageNumber { get; private set; } = 1;

    public string StatusFilter { get; private set; } = HelpRequestStatus.Open;

    public async Task OnGetAsync(string? status = HelpRequestStatus.Open, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        StatusFilter = string.IsNullOrWhiteSpace(status) ? HelpRequestStatus.Open : status;
        List = await helpRequestRepository.ListAsync(StatusFilter, PageNumber, 50, cancellationToken);
        ViewData["Title"] = "Help requests";
    }
}
