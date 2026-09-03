using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class IndexModel(IArticleSubmissionRepository articleSubmissionRepository, IEditorialArticleRepository editorialArticles, IArticlesRepository legacyArticles) : AdminArticlesPageModel
{
    public IReadOnlyList<ArticleSubmissionListItem> Submissions { get; private set; } = [];
    public IReadOnlyList<EditorialArticle> EditorialArticles { get; private set; } = [];
    public IReadOnlyList<ArticleItem> LegacyArticles { get; private set; } = [];

    public async Task OnGetAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        Submissions = await articleSubmissionRepository.GetPendingAsync(Math.Max(1, page), 50, cancellationToken);
        EditorialArticles = await editorialArticles.GetAllAsync(cancellationToken);
        LegacyArticles = await LoadAllLegacyArchiveAsync(legacyArticles, cancellationToken);
        ViewData["Title"] = "Articles";
    }
}
