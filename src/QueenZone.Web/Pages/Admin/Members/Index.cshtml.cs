using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Pages.Admin.Members;

public sealed class IndexModel(
    IMemberAccountRepository memberAccountRepository,
    IForumWriteRepository forumWriteRepository) : AdminMembersPageModel
{
    private const int PageSize = 50;

    public IReadOnlyList<MemberAccount> Members { get; private set; } = [];

    public AuthorForumContentCounts? NoAccountAuthor { get; private set; }

    public string? Query { get; private set; }

    public int PageNumber { get; private set; } = 1;

    public int TotalCount { get; private set; }

    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task OnGetAsync(string? query, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        PageNumber = Math.Max(1, pageNumber);

        var result = await memberAccountRepository.SearchMembersAsync(Query, PageNumber, PageSize, cancellationToken);
        Members = result.Members;
        TotalCount = result.TotalCount;
        if (Members.Count == 0 && Query is not null)
        {
            NoAccountAuthor = await forumWriteRepository.FindForumAuthorByDisplayNameAsync(Query, cancellationToken);
        }

        StatusMessage = TempData["MemberMessage"] as string;
        StatusMessageKind = TempData["MemberMessageKind"] as string;
        ViewData["Title"] = "Members";
    }
}
