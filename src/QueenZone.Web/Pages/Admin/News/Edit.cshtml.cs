using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class EditModel(IAdminNewsRepository adminNewsRepository) : AdminNewsPageModel
{
    public ArticleFormViewModel? Form { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Edit: {article.Title}";
        Form = BuildForm(article, ToDraft(article), null);
        return Page();
    }

    public static ArticleFormViewModel BuildForm(
        AdminNewsArticle article,
        AdminNewsDraft draft,
        IReadOnlyList<string>? errors) =>
        new($"Edit: {article.Title}", $"/admin/news/{article.Id}", draft, errors, article);
}
