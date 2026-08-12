using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Admin.Members;

public sealed class IndexModel(IMemberAccountRepository memberAccountRepository) : AdminMembersPageModel
{
    private const int PageSize = 50;

    public IReadOnlyList<MemberAccount> Members { get; private set; } = [];

    public string? Query { get; private set; }

    public int PageNumber { get; private set; } = 1;

    public int TotalCount { get; private set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task OnGetAsync(string? query, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        PageNumber = Math.Max(1, pageNumber);

        var result = await memberAccountRepository.SearchMembersAsync(Query, PageNumber, PageSize, cancellationToken);
        Members = result.Members;
        TotalCount = result.TotalCount;
        ViewData["Title"] = "Members";
    }
}
