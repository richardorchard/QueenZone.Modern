using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public sealed class EditModel(
    IAdminNewsRepository adminNewsRepository,
    INewsDiscoveryRepository discoveryRepository,
    ILogger<EditModel> logger) : AdminNewsPageModel
{
    public ArticleFormViewModel? Form { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? StatusMessageKind { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return NotFound();
        }

        StatusMessage = TempData[AdminNewsMessages.MessageKey] as string;
        StatusMessageKind = TempData[AdminNewsMessages.MessageKindKey] as string;

        NewsDiscoveryProvenance? provenance = null;
        try
        {
            provenance = await NewsDiscoveryProvenanceBuilder.LoadForPromotedArticleAsync(
                discoveryRepository,
                id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load discovery provenance for admin news {NewsId}", id);
        }

        ViewData["Title"] = "Edit article";
        Form = BuildForm(article, ToDraft(article), null, provenance);
        return Page();
    }

    public static ArticleFormViewModel BuildForm(
        AdminNewsArticle article,
        AdminNewsDraft draft,
        IReadOnlyList<string>? errors,
        NewsDiscoveryProvenance? discoveryProvenance = null) =>
        new(
            "Edit article",
            $"/admin/news/{article.Id}",
            draft,
            errors,
            article,
            discoveryProvenance,
            article.Title);
}
