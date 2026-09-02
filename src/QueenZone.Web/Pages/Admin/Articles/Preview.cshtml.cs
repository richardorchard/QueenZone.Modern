using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.Articles;

public sealed class PreviewModel(IEditorialArticleRepository articles, UgcHtml ugcHtml) : AdminArticlesPageModel
{
    public EditorialArticle? Article { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct) { Article = await articles.GetAsync(id, ct); if (Article is null) return NotFound(); Body = ugcHtml.FormatForDisplay(Article.Body); return Page(); }
}
