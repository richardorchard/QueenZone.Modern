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

    protected static NewsItem ToNewsItem(AdminNewsArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl,
            article.IsPublished,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug);

    protected string EditorEmail =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("preferred_username")
        ?? User.Identity?.Name
        ?? "unknown";
}
