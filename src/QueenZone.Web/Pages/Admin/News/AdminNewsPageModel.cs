using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QueenZone.Data;

namespace QueenZone.Web.Pages.Admin.News;

public abstract class AdminNewsPageModel : PageModel
{
    public const string AntiforgeryTokenFieldName = "__RequestVerificationToken";

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        ViewData["ShowAdminNav"] = true;
        base.OnPageHandlerExecuting(context);
    }

    protected static AdminNewsDraft ToDraft(AdminNewsArticle article) =>
        new(
            article.Title,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl);

    protected static NewsDetailItem ToNewsDetailItem(AdminNewsArticle article) =>
        PublicContentMapper.ToNewsDetailItem(article);

    protected string EditorEmail =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("preferred_username")
        ?? User.Identity?.Name
        ?? "unknown";
}
