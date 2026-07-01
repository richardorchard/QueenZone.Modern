using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class EditModel(
    IAdminNewsRepository adminNewsRepository,
    INewsDiscoveryRepository discoveryRepository) : AdminNewsPageModel
{
    public ArticleFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        var provenance = await NewsDiscoveryProvenanceBuilder.LoadForPromotedArticleAsync(
            discoveryRepository,
            id,
            cancellationToken);

        ViewData["Title"] = $"Edit: {article.Title}";
        Form = BuildForm(article, ToDraft(article), null, provenance);
        return Page();
    }

    public static ArticleFormViewModel BuildForm(
        AdminNewsArticle article,
        AdminNewsDraft draft,
        IReadOnlyList<string>? errors,
        NewsDiscoveryProvenance? discoveryProvenance = null) =>
        new($"Edit: {article.Title}", $"/admin/news/{article.Id}", draft, errors, article, discoveryProvenance);
}
