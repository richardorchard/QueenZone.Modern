using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class PreviewModel(IAdminNewsRepository adminNewsRepository) : AdminNewsPageModel
{
    public AdminNewsArticle? Article { get; private set; }

    public NewsItem? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (Article is null)
        {
            return NotFound();
        }

        Item = ToNewsItem(Article);
        ViewData["Title"] = $"Preview: {Article.Title}";
        ViewData["CanonicalPath"] = NewsArticleContent.GetDetailCanonicalPath(Item.Id, Item.Title, Item.Slug);
        ViewData["Description"] = Item.Excerpt;
        return Page();
    }
}
