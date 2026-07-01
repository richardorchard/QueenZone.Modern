using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class PreviewModel(
    IAdminNewsRepository adminNewsRepository,
    INewsDiscoveryRepository discoveryRepository,
    ILogger<PreviewModel> logger) : AdminNewsPageModel
{
    public AdminNewsArticle? Article { get; private set; }

    public NewsItem? Item { get; private set; }

    public NewsDiscoveryProvenance? DiscoveryProvenance { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (Article is null)
        {
            return NotFound();
        }

        try
        {
            DiscoveryProvenance = await NewsDiscoveryProvenanceBuilder.LoadForPromotedArticleAsync(
                discoveryRepository,
                id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load discovery provenance for admin news preview {NewsId}", id);
        }

        Item = ToNewsItem(Article);
        ViewData["Title"] = $"Preview: {Article.Title}";
        ViewData["CanonicalPath"] = NewsArticleContent.GetDetailCanonicalPath(Item.Id, Item.Title, Item.Slug);
        ViewData["Description"] = Item.Excerpt;
        return Page();
    }
}
