using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class IndexModel(IArticleSubmissionRepository articleSubmissionRepository) : AdminArticlesPageModel
{
    public IReadOnlyList<ArticleSubmissionListItem> Submissions { get; private set; } = [];

    public async Task OnGetAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        Submissions = await articleSubmissionRepository.GetPendingAsync(Math.Max(1, page), 50, cancellationToken);
        ViewData["Title"] = "Article submissions";
    }
}
