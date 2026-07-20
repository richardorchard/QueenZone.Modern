using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Articles;

public sealed class CommunityDetailModel(
    IArticleSubmissionRepository articleSubmissionRepository,
    UgcHtml ugcHtml) : PageModel
{
    public PublishedArticleSubmission? Item { get; private set; }

    public string FormattedBody { get; private set; } = string.Empty;

    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        var published = await articleSubmissionRepository.GetPublishedAsync(cancellationToken);
        var item = published.FirstOrDefault(a => string.Equals(a.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            return NotFound();
        }

        Item = item;
        FormattedBody = ugcHtml.FormatForDisplay(item.Body);

        Breadcrumbs =
        [
            BreadcrumbItem.Home,
            new BreadcrumbItem("Articles", "/articles"),
            new BreadcrumbItem(item.Title, ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug)),
        ];

        ViewData["Title"] = $"{item.Title} | QueenZone articles";
        ViewData["CanonicalPath"] = ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug);
        ViewData["Description"] = item.Excerpt;

        return Page();
    }
}
