using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.TriviaSubmissions;

public sealed class IndexModel(ITriviaFactSubmissionRepository triviaFactSubmissionRepository)
    : AdminTriviaSubmissionsPageModel
{
    public IReadOnlyList<TriviaFactSubmissionListItem> Submissions { get; private set; } = [];

    public int PageNumber { get; private set; } = 1;

    public async Task OnGetAsync(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        PageNumber = Math.Max(1, pageNumber);
        Submissions = await triviaFactSubmissionRepository.GetPendingAsync(PageNumber, 50, cancellationToken);
        ViewData["Title"] = "Trivia submissions";
    }
}
