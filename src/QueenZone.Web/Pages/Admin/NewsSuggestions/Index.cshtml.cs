using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.NewsSuggestions;

public sealed class IndexModel(INewsSuggestionRepository newsSuggestionRepository) : AdminNewsSuggestionsPageModel
{
    public IReadOnlyList<NewsSuggestionListItem> Suggestions { get; private set; } = [];

    public int PageNumber { get; private set; } = 1;

    public async Task OnGetAsync(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        Suggestions = await newsSuggestionRepository.GetPendingAsync(PageNumber, 50, cancellationToken);
        ViewData["Title"] = "News suggestions";
    }
}
