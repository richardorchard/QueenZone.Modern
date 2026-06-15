using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web;

public static class AdminNewsRoutes
{
    public static IEndpointRouteBuilder MapAdminNewsRoutes(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/admin/news").RequireAuthorization("Admin");

        admin.MapGet("/", ListArticlesAsync);
        admin.MapGet("/new", RenderCreateFormAsync);
        admin.MapPost("/", CreateArticleAsync);
        admin.MapGet("/{id:int}/edit", RenderEditFormAsync);
        admin.MapPost("/{id:int}", UpdateArticleAsync);
        admin.MapGet("/{id:int}/preview", PreviewArticleAsync);
        admin.MapPost("/{id:int}/publish", PublishArticleAsync);
        admin.MapPost("/{id:int}/unpublish", UnpublishArticleAsync);
        admin.MapPost("/{id:int}/delete", DeleteArticleAsync);

        return endpoints;
    }

    private static async Task<IResult> ListArticlesAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        CancellationToken cancellationToken)
    {
        var token = GetAntiforgeryToken(httpContext, antiforgery);
        var articles = await adminNewsRepository.GetAllAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.Append("<h1>Admin news</h1>");
        builder.Append("<p><a href=\"/admin/news/new\">Create article</a></p>");

        if (articles.Count == 0)
        {
            builder.Append("<p>No news articles yet.</p>");
            return Results.Content(RenderAdminPage("Admin news", builder.ToString()), "text/html; charset=utf-8");
        }

        builder.Append("<table class=\"admin-table\"><thead><tr><th>Title</th><th>Status</th><th>Published</th><th>Actions</th></tr></thead><tbody>");
        foreach (var article in articles)
        {
            builder.Append("<tr><td>");
            builder.Append(WebUtility.HtmlEncode(article.Title));
            builder.Append("</td><td>");
            builder.Append(article.IsPublished ? "Published" : "Draft");
            builder.Append("</td><td>");
            builder.Append(article.PublishedAt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
            builder.Append("</td><td class=\"admin-actions\">");
            builder.Append($"<a href=\"/admin/news/{article.Id}/edit\">Edit</a> ");
            builder.Append($"<a href=\"/admin/news/{article.Id}/preview\">Preview</a> ");
            if (article.IsPublished)
            {
                builder.Append(RenderPostLink($"/admin/news/{article.Id}/unpublish", "Unpublish", token));
            }
            else
            {
                builder.Append(RenderPostLink($"/admin/news/{article.Id}/publish", "Publish", token));
            }

            builder.Append(' ');
            builder.Append(RenderPostLink($"/admin/news/{article.Id}/delete", "Delete", token, confirm: true));
            builder.Append("</td></tr>");
        }

        builder.Append("</tbody></table>");
        return Results.Content(RenderAdminPage("Admin news", builder.ToString()), "text/html; charset=utf-8");
    }

    private static async Task<IResult> RenderCreateFormAsync(HttpContext httpContext, IAntiforgery antiforgery)
    {
        var token = GetAntiforgeryToken(httpContext, antiforgery);
        var body = RenderArticleForm(
            title: "Create news article",
            action: "/admin/news",
            token: token,
            draft: new AdminNewsDraft(
                string.Empty,
                null,
                string.Empty,
                string.Empty,
                DateTime.UtcNow.Date,
                null),
            errors: null);
        return Results.Content(RenderAdminPage("Create news article", body), "text/html; charset=utf-8");
    }

    private static async Task<IResult> CreateArticleAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        INewsAuditRepository auditRepository,
        [FromForm] AdminNewsForm form,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var draft = form.ToDraft();
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(NewsSlug.Resolve(draft.Title, draft.Slug), cancellationToken: cancellationToken);
        var errors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (errors.Count > 0)
        {
            var token = GetAntiforgeryToken(httpContext, antiforgery);
            var body = RenderArticleForm("Create news article", "/admin/news", token, draft, errors);
            return Results.Content(RenderAdminPage("Create news article", body), "text/html; charset=utf-8");
        }

        var editorEmail = GetEditorEmail(httpContext.User);
        var id = await adminNewsRepository.CreateDraftAsync(draft, editorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "create", editorEmail, $"Created draft \"{draft.Title}\"", cancellationToken);
        return Results.Redirect($"/admin/news/{id}/edit");
    }

    private static async Task<IResult> RenderEditFormAsync(
        int id,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var token = GetAntiforgeryToken(httpContext, antiforgery);
        var body = RenderArticleForm(
            title: $"Edit: {article.Title}",
            action: $"/admin/news/{id}",
            token: token,
            draft: ToDraft(article),
            errors: null,
            article: article);
        return Results.Content(RenderAdminPage($"Edit: {article.Title}", body), "text/html; charset=utf-8");
    }

    private static async Task<IResult> UpdateArticleAsync(
        int id,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        INewsAuditRepository auditRepository,
        [FromForm] AdminNewsForm form,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var existing = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Results.NotFound();
        }

        var draft = form.ToDraft();
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(
            NewsSlug.Resolve(draft.Title, draft.Slug),
            excludeNewsId: id,
            cancellationToken: cancellationToken);
        var errors = NewsValidation.ValidateDraft(draft, slugInUse);
        if (errors.Count > 0)
        {
            var token = GetAntiforgeryToken(httpContext, antiforgery);
            var body = RenderArticleForm($"Edit: {existing.Title}", $"/admin/news/{id}", token, draft, errors, existing);
            return Results.Content(RenderAdminPage($"Edit: {existing.Title}", body), "text/html; charset=utf-8");
        }

        var editorEmail = GetEditorEmail(httpContext.User);
        await adminNewsRepository.UpdateAsync(id, draft, editorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "edit", editorEmail, $"Updated \"{draft.Title}\"", cancellationToken);
        return Results.Redirect($"/admin/news/{id}/edit");
    }

    private static async Task<IResult> PreviewArticleAsync(
        int id,
        IAdminNewsRepository adminNewsRepository,
        CancellationToken cancellationToken)
    {
        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var newsItem = ToNewsItem(article);
        var slug = NewsSlug.ResolveForArticle(newsItem);
        var body = new StringBuilder();
        body.Append("<p class=\"admin-preview-banner\">Preview mode. ");
        body.Append(article.IsPublished ? "This article is published." : "This article is not published.");
        body.Append("</p>");
        body.Append("<p class=\"article-nav\"><a href=\"/admin/news\">Back to admin</a> | ");
        body.Append($"<a href=\"/admin/news/{id}/edit\">Edit</a></p>");
        body.Append(RenderPublicNewsDetail(newsItem));
        if (!article.IsPublished)
        {
            body.Append("<p><em>This draft is hidden from the public archive.</em></p>");
        }

        var metadata = new PublicPageHeadMetadata(
            CanonicalPath: NewsArticleContent.GetDetailCanonicalPath(newsItem.Id, newsItem.Title, newsItem.Slug),
            Description: newsItem.Excerpt);

        return Results.Content(
            RenderPublicPage($"Preview: {article.Title}", body.ToString(), metadata),
            "text/html; charset=utf-8");
    }

    private static async Task<IResult> PublishArticleAsync(
        int id,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        INewsAuditRepository auditRepository,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var editorEmail = GetEditorEmail(httpContext.User);
        await adminNewsRepository.PublishAsync(id, editorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "publish", editorEmail, $"Published \"{article.Title}\"", cancellationToken);
        return Results.Redirect("/admin/news");
    }

    private static async Task<IResult> UnpublishArticleAsync(
        int id,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        INewsAuditRepository auditRepository,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var editorEmail = GetEditorEmail(httpContext.User);
        await adminNewsRepository.UnpublishAsync(id, editorEmail, cancellationToken);
        await auditRepository.AppendAsync(id, "unpublish", editorEmail, $"Unpublished \"{article.Title}\"", cancellationToken);
        return Results.Redirect("/admin/news");
    }

    private static async Task<IResult> DeleteArticleAsync(
        int id,
        HttpContext httpContext,
        IAntiforgery antiforgery,
        IAdminNewsRepository adminNewsRepository,
        INewsAuditRepository auditRepository,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(httpContext);

        var article = await adminNewsRepository.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var editorEmail = GetEditorEmail(httpContext.User);
        await auditRepository.AppendAsync(id, "delete", editorEmail, $"Deleted \"{article.Title}\"", cancellationToken);
        await adminNewsRepository.DeleteAsync(id, editorEmail, cancellationToken);
        return Results.Redirect("/admin/news");
    }

    private static string RenderArticleForm(
        string title,
        string action,
        string token,
        AdminNewsDraft draft,
        IReadOnlyList<string>? errors,
        AdminNewsArticle? article = null)
    {
        var builder = new StringBuilder();
        builder.Append($"<h1>{WebUtility.HtmlEncode(title)}</h1>");

        if (errors is { Count: > 0 })
        {
            builder.Append("<ul class=\"admin-errors\">");
            foreach (var error in errors)
            {
                builder.Append("<li>");
                builder.Append(WebUtility.HtmlEncode(error));
                builder.Append("</li>");
            }

            builder.Append("</ul>");
        }

        builder.Append($"<form method=\"post\" action=\"{WebUtility.HtmlEncode(action)}\" class=\"admin-form\">");
        builder.Append(AntiforgeryField(token));
        builder.Append(FormField("Title", "title", draft.Title, required: true));
        builder.Append(FormField(
            "Slug",
            "slug",
            draft.Slug ?? string.Empty,
            placeholder: NewsSlug.Resolve(draft.Title, null),
            helpText: "Leave blank to auto-generate from the title."));
        builder.Append(FormField("Excerpt", "excerpt", draft.Excerpt, required: true));
        builder.Append(FormTextArea("Body", "body", draft.Body));
        builder.Append(FormField(
            "Publication date",
            "publishedAt",
            draft.PublishedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            type: "date",
            required: true));
        builder.Append(FormField("Source URL", "sourceUrl", draft.SourceUrl ?? string.Empty));

        builder.Append("<div class=\"admin-form-actions\">");
        builder.Append("<button type=\"submit\">Save</button> ");
        builder.Append("<a href=\"/admin/news\">Cancel</a>");
        if (article is not null)
        {
            builder.Append($" | <a href=\"/admin/news/{article.Id}/preview\">Preview</a>");
            if (article.IsPublished)
            {
                builder.Append(' ');
                builder.Append(RenderPostLink($"/admin/news/{article.Id}/unpublish", "Unpublish", token));
            }
            else
            {
                builder.Append(' ');
                builder.Append(RenderPostLink($"/admin/news/{article.Id}/publish", "Publish", token));
            }
        }

        builder.Append("</div></form>");
        return builder.ToString();
    }

    private static string FormField(
        string label,
        string name,
        string value,
        bool required = false,
        string type = "text",
        string? placeholder = null,
        string? helpText = null)
    {
        var builder = new StringBuilder();
        builder.Append("<label>");
        builder.Append(WebUtility.HtmlEncode(label));
        builder.Append($"<input type=\"{type}\" name=\"{name}\" value=\"");
        builder.Append(WebUtility.HtmlEncode(value));
        builder.Append('"');
        if (required)
        {
            builder.Append(" required");
        }

        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            builder.Append(" placeholder=\"");
            builder.Append(WebUtility.HtmlEncode(placeholder));
            builder.Append('"');
        }

        builder.Append("></label>");
        if (!string.IsNullOrWhiteSpace(helpText))
        {
            builder.Append("<p class=\"admin-help\">");
            builder.Append(WebUtility.HtmlEncode(helpText));
            builder.Append("</p>");
        }

        return builder.ToString();
    }

    private static string FormTextArea(string label, string name, string value)
    {
        return $"""
            <label>{WebUtility.HtmlEncode(label)}
            <textarea name="{name}" rows="16">{WebUtility.HtmlEncode(value)}</textarea>
            </label>
            """;
    }

    private static string AntiforgeryField(string token) =>
        $"""<input type="hidden" name="{AntiforgeryTokenFieldName}" value="{WebUtility.HtmlEncode(token)}">""";

    private static string RenderPostLink(string action, string label, string token, bool confirm = false)
    {
        var builder = new StringBuilder();
        builder.Append($"<form method=\"post\" action=\"{WebUtility.HtmlEncode(action)}\" class=\"admin-inline-form\"");
        if (confirm)
        {
            builder.Append(" onsubmit=\"return confirm('Are you sure?');\"");
        }

        builder.Append('>');
        builder.Append(AntiforgeryField(token));
        builder.Append("<button type=\"submit\">");
        builder.Append(WebUtility.HtmlEncode(label));
        builder.Append("</button></form>");
        return builder.ToString();
    }

    private static string GetAntiforgeryToken(HttpContext httpContext, IAntiforgery antiforgery) =>
        antiforgery.GetAndStoreTokens(httpContext).RequestToken ?? string.Empty;

    private static string GetEditorEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email)
        ?? user.FindFirstValue("preferred_username")
        ?? user.Identity?.Name
        ?? "unknown";

    private static AdminNewsDraft ToDraft(AdminNewsArticle article) =>
        new(
            article.Title,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl);

    private static NewsItem ToNewsItem(AdminNewsArticle article) =>
        new(
            article.Id,
            article.Title,
            article.Excerpt,
            article.Body,
            article.PublishedAt,
            article.SourceUrl,
            article.IsPublished,
            string.IsNullOrWhiteSpace(article.Slug) ? null : article.Slug);

    private static string RenderPublicNewsDetail(NewsItem item)
    {
        var builder = new StringBuilder();
        builder.Append($"<h1>{WebUtility.HtmlEncode(item.Title)}</h1>");
        builder.Append("<p class=\"date\"><time datetime=\"");
        builder.Append(item.PublishedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        builder.Append("\">");
        builder.Append(item.PublishedAt.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture));
        builder.Append("</time></p>");
        builder.Append("<p class=\"lede\">");
        builder.Append(WebUtility.HtmlEncode(item.Excerpt));
        builder.Append("</p>");
        builder.Append("<article class=\"article-body\">");
        builder.Append(NewsArticleContent.FormatBody(item.Body));
        builder.Append("</article>");

        if (NewsValidation.IsSafePublicUrl(item.SourceUrl))
        {
            builder.Append("<p class=\"article-source\"><a href=\"");
            builder.Append(WebUtility.HtmlEncode(item.SourceUrl));
            builder.Append("\" rel=\"noopener noreferrer\">Source</a></p>");
        }

        return builder.ToString();
    }

    private static string RenderAdminPage(string title, string body) =>
        RenderSharedPage(title, body, isAdmin: true);

    private static string RenderPublicPage(string title, string body, PublicPageHeadMetadata? metadata = null)
    {
        var headExtras = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(metadata?.CanonicalPath))
        {
            headExtras.Append("  <link rel=\"canonical\" href=\"");
            headExtras.Append(WebUtility.HtmlEncode(metadata.CanonicalPath));
            headExtras.Append("\">\n");
        }

        if (!string.IsNullOrWhiteSpace(metadata?.Description))
        {
            headExtras.Append("  <meta name=\"description\" content=\"");
            headExtras.Append(WebUtility.HtmlEncode(metadata.Description));
            headExtras.Append("\">\n");
        }

        return RenderSharedPage(title, body, isAdmin: false, headExtras.ToString());
    }

    private static string RenderSharedPage(string title, string body, bool isAdmin, string headExtras = "")
    {
        var adminNav = isAdmin
            ? "<a href=\"/admin/news\">Admin news</a>"
            : string.Empty;

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{WebUtility.HtmlEncode(title)}}</title>
            {{headExtras}}  <style>
                body { font-family: Arial, sans-serif; line-height: 1.5; margin: 0 auto; max-width: 880px; padding: 2rem; }
                header { border-bottom: 1px solid #d0d7de; margin-bottom: 2rem; padding-bottom: 1rem; }
                header nav a { margin-right: 1rem; }
                .admin-table { border-collapse: collapse; width: 100%; }
                .admin-table th, .admin-table td { border-bottom: 1px solid #d0d7de; padding: 0.5rem; text-align: left; vertical-align: top; }
                .admin-actions form { display: inline; margin: 0; }
                .admin-inline-form { display: inline; margin: 0 0.25rem; }
                .admin-form label { display: block; margin-bottom: 1rem; }
                .admin-form input, .admin-form textarea { display: block; margin-top: 0.35rem; width: 100%; }
                .admin-form textarea { font-family: Consolas, monospace; }
                .admin-form-actions { margin-top: 1.5rem; }
                .admin-errors { background: #fff8f8; border: 1px solid #ff8182; color: #a40e26; padding: 0.75rem 1rem; }
                .admin-help { color: #57606a; font-size: 0.9rem; margin: -0.5rem 0 1rem; }
                .admin-preview-banner { background: #fff8c5; border: 1px solid #d4a72c; padding: 0.75rem 1rem; }
                .date, time { color: #57606a; }
                .lede { font-size: 1.15rem; }
                .article-body { margin-top: 1.5rem; }
              </style>
            </head>
            <body>
              <header>
                <strong>QueenZone</strong>
                <nav><a href="/">Home</a><a href="/news">News</a>{{adminNav}}</nav>
              </header>
              <main>
                {{body}}
              </main>
            </body>
            </html>
            """;
    }

    private sealed record PublicPageHeadMetadata(string? CanonicalPath = null, string? Description = null);

    public sealed class AdminNewsForm
    {
        [FromForm(Name = "title")]
        public string Title { get; init; } = string.Empty;

        [FromForm(Name = "slug")]
        public string? Slug { get; init; }

        [FromForm(Name = "excerpt")]
        public string Excerpt { get; init; } = string.Empty;

        [FromForm(Name = "body")]
        public string Body { get; init; } = string.Empty;

        [FromForm(Name = "publishedAt")]
        public string PublishedAt { get; init; } = string.Empty;

        [FromForm(Name = "sourceUrl")]
        public string? SourceUrl { get; init; }

        public AdminNewsDraft ToDraft()
        {
            DateTime publishedAt = default;
            if (!string.IsNullOrWhiteSpace(PublishedAt)
                && DateTime.TryParse(PublishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                publishedAt = parsed;
            }

            return new AdminNewsDraft(
                Title.Trim(),
                string.IsNullOrWhiteSpace(Slug) ? null : Slug.Trim(),
                Excerpt.Trim(),
                Body,
                publishedAt,
                string.IsNullOrWhiteSpace(SourceUrl) ? null : SourceUrl.Trim());
        }
    }

    public const string AntiforgeryTokenFieldName = "__RequestVerificationToken";
}