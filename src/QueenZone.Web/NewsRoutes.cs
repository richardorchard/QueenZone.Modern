using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using QueenZone.Data;

namespace QueenZone.Web;

public static partial class NewsRoutes
{
    public const int ArchivePageSize = 20;

    public static IEndpointRouteBuilder MapNewsRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", async (INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            var latest = await newsRepository.GetLatestAsync(5, cancellationToken);
            return Results.Content(RenderPage("QueenZone", RenderNewsList("Latest news", latest)), "text/html; charset=utf-8");
        });

        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        endpoints.MapGet("/news", async (INewsRepository newsRepository, CancellationToken cancellationToken) =>
            await RenderArchivePageAsync(1, newsRepository, cancellationToken));

        endpoints.MapGet("/news/page/{page:int}", async (int page, INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            if (page == 1)
            {
                return Results.Redirect("/news", permanent: true);
            }

            return await RenderArchivePageAsync(page, newsRepository, cancellationToken);
        });

        endpoints.MapGet("/news/{id:int}/{slug}", async (int id, string slug, INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            var item = await newsRepository.GetByIdAsync(id, cancellationToken);
            if (item is null)
            {
                return Results.NotFound();
            }

            var canonicalSlug = Slugify(item.Title);
            if (!string.Equals(canonicalSlug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Redirect($"/news/{item.Id}/{canonicalSlug}", permanent: true);
            }

            return Results.Content(RenderPage(item.Title, RenderNewsDetail(item)), "text/html; charset=utf-8");
        });

        return endpoints;
    }

    public static int GetArchiveTotalPages(int publishedCount, int pageSize = ArchivePageSize)
    {
        if (publishedCount <= 0)
        {
            return 0;
        }

        return (publishedCount + pageSize - 1) / pageSize;
    }

    public static string GetArchiveCanonicalPath(int page) =>
        page <= 1 ? "/news" : $"/news/page/{page}";

    public static string GetArchivePageTitle(int page) =>
        page <= 1 ? "QueenZone news" : $"QueenZone news – Page {page}";

    public static string BuildArchivePaginationNav(int currentPage, int totalPages)
    {
        if (totalPages <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<nav class=\"archive-pagination\" aria-label=\"News archive pagination\">");
        builder.Append("<p class=\"archive-pagination-summary\">Page ");
        builder.Append(currentPage);
        builder.Append(" of ");
        builder.Append(totalPages);
        builder.Append("</p>");
        builder.Append("<div class=\"archive-pagination-controls\">");
        builder.Append(RenderArchivePaginationPrevious(currentPage));
        builder.Append("<ol class=\"archive-pagination-pages\">");

        foreach (var pageNumber in GetVisiblePageNumbers(currentPage, totalPages))
        {
            builder.Append("<li>");
            if (pageNumber is null)
            {
                builder.Append("<span class=\"archive-pagination-ellipsis\" aria-hidden=\"true\">…</span>");
            }
            else if (pageNumber == currentPage)
            {
                builder.Append("<span aria-current=\"page\">");
                builder.Append(pageNumber);
                builder.Append("</span>");
            }
            else
            {
                builder.Append("<a href=\"");
                builder.Append(WebUtility.HtmlEncode(GetArchiveCanonicalPath(pageNumber.Value)));
                builder.Append("\">");
                builder.Append(pageNumber);
                builder.Append("</a>");
            }

            builder.Append("</li>");
        }

        builder.Append("</ol>");
        builder.Append(RenderArchivePaginationNext(currentPage, totalPages));
        builder.Append("</div></nav>");
        return builder.ToString();
    }

    private static string RenderArchivePaginationPrevious(int currentPage)
    {
        if (currentPage <= 1)
        {
            return "<span class=\"archive-pagination-prev is-disabled\" aria-disabled=\"true\">Previous</span>";
        }

        var previousPath = WebUtility.HtmlEncode(GetArchiveCanonicalPath(currentPage - 1));
        return $"<a class=\"archive-pagination-prev\" rel=\"prev\" href=\"{previousPath}\">Previous</a>";
    }

    private static string RenderArchivePaginationNext(int currentPage, int totalPages)
    {
        if (currentPage >= totalPages)
        {
            return "<span class=\"archive-pagination-next is-disabled\" aria-disabled=\"true\">Next</span>";
        }

        var nextPath = WebUtility.HtmlEncode(GetArchiveCanonicalPath(currentPage + 1));
        return $"<a class=\"archive-pagination-next\" rel=\"next\" href=\"{nextPath}\">Next</a>";
    }

    public static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var replaced = NonAlphaNumericRegex().Replace(lower, "-");
        return DuplicateDashRegex().Replace(replaced, "-").Trim('-');
    }

    public static int ResolveArchiveTotalPages(int currentPage, int itemCount, int publishedCount, int totalPages)
    {
        if (itemCount == ArchivePageSize && totalPages <= currentPage)
        {
            return Math.Max(totalPages, currentPage + 1);
        }

        if (publishedCount > 0)
        {
            return totalPages;
        }

        return itemCount == 0 ? 0 : Math.Max(totalPages, currentPage);
    }

    private static async Task<IResult> RenderArchivePageAsync(
        int page,
        INewsRepository newsRepository,
        CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            return Results.NotFound();
        }

        var publishedCount = await newsRepository.GetPublishedCountAsync(cancellationToken);
        var archive = await newsRepository.GetArchivePageAsync(page, ArchivePageSize, cancellationToken);
        var totalPages = ResolveArchiveTotalPages(
            page,
            archive.Count,
            publishedCount,
            GetArchiveTotalPages(publishedCount));

        if (totalPages == 0)
        {
            if (page > 1)
            {
                return Results.NotFound();
            }
        }
        else if (page > totalPages)
        {
            return Results.NotFound();
        }

        var body = RenderNewsList("News archive", archive);
        body += BuildArchivePaginationNav(page, totalPages);

        var metadata = new PageHeadMetadata(
            CanonicalPath: GetArchiveCanonicalPath(page),
            PrevPath: page > 1 ? GetArchiveCanonicalPath(page - 1) : null,
            NextPath: totalPages > 0 && page < totalPages ? GetArchiveCanonicalPath(page + 1) : null);

        return Results.Content(
            RenderPage(GetArchivePageTitle(page), body, metadata),
            "text/html; charset=utf-8");
    }

    private static IEnumerable<int?> GetVisiblePageNumbers(int currentPage, int totalPages)
    {
        if (totalPages <= 7)
        {
            for (var page = 1; page <= totalPages; page++)
            {
                yield return page;
            }

            yield break;
        }

        yield return 1;

        if (currentPage > 3)
        {
            yield return null;
        }

        var start = Math.Max(2, currentPage - 1);
        var end = Math.Min(totalPages - 1, currentPage + 1);
        for (var page = start; page <= end; page++)
        {
            yield return page;
        }

        if (currentPage < totalPages - 2)
        {
            yield return null;
        }

        yield return totalPages;
    }

    private static string RenderNewsList(string heading, IReadOnlyList<NewsItem> items)
    {
        var builder = new StringBuilder();
        builder.Append(CultureSafeHeading(heading));

        if (items.Count == 0)
        {
            builder.Append("<p>No published news is available yet.</p>");
            return builder.ToString();
        }

        builder.Append("<ol class=\"news-list\">");
        foreach (var item in items)
        {
            builder.Append("<li>");
            builder.Append(CultureSafeLink(item));
            builder.Append("<p>");
            builder.Append(WebUtility.HtmlEncode(item.Excerpt));
            builder.Append("</p>");
            builder.Append("<time datetime=\"");
            builder.Append(item.PublishedAt.ToString("yyyy-MM-dd"));
            builder.Append("\">");
            builder.Append(item.PublishedAt.ToString("dd MMMM yyyy"));
            builder.Append("</time>");
            builder.Append("</li>");
        }

        builder.Append("</ol>");
        return builder.ToString();
    }

    private static string RenderNewsDetail(NewsItem item)
    {
        var builder = new StringBuilder();
        builder.Append(CultureSafeHeading(item.Title));
        builder.Append("<p class=\"date\">");
        builder.Append(item.PublishedAt.ToString("dd MMMM yyyy"));
        builder.Append("</p>");
        builder.Append("<p class=\"lede\">");
        builder.Append(WebUtility.HtmlEncode(item.Excerpt));
        builder.Append("</p>");
        builder.Append("<article>");
        builder.Append(WebUtility.HtmlEncode(item.Body).Replace("\n", "<br>"));
        builder.Append("</article>");

        if (!string.IsNullOrWhiteSpace(item.SourceUrl))
        {
            builder.Append("<p><a href=\"");
            builder.Append(WebUtility.HtmlEncode(item.SourceUrl));
            builder.Append("\">Source</a></p>");
        }

        return builder.ToString();
    }

    private static string RenderPage(string title, string body, PageHeadMetadata? metadata = null)
    {
        var headExtras = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(metadata?.CanonicalPath))
        {
            headExtras.Append("  <link rel=\"canonical\" href=\"");
            headExtras.Append(WebUtility.HtmlEncode(metadata.CanonicalPath));
            headExtras.Append("\">\n");
        }

        if (!string.IsNullOrWhiteSpace(metadata?.PrevPath))
        {
            headExtras.Append("  <link rel=\"prev\" href=\"");
            headExtras.Append(WebUtility.HtmlEncode(metadata.PrevPath));
            headExtras.Append("\">\n");
        }

        if (!string.IsNullOrWhiteSpace(metadata?.NextPath))
        {
            headExtras.Append("  <link rel=\"next\" href=\"");
            headExtras.Append(WebUtility.HtmlEncode(metadata.NextPath));
            headExtras.Append("\">\n");
        }

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
                .news-list { padding-left: 1.5rem; }
                .news-list li { margin-bottom: 1.5rem; }
                .date, time { color: #57606a; }
                .lede { font-size: 1.15rem; }
                .archive-pagination { border-top: 1px solid #d0d7de; margin-top: 2.5rem; padding-top: 1.5rem; }
                .archive-pagination-summary { color: #57606a; margin: 0 0 1rem; }
                .archive-pagination-controls { align-items: center; display: flex; flex-wrap: wrap; gap: 0.75rem 1rem; }
                .archive-pagination-pages { display: flex; flex-wrap: wrap; gap: 0.5rem; list-style: none; margin: 0; padding: 0; }
                .archive-pagination-pages a,
                .archive-pagination-prev,
                .archive-pagination-next { border: 1px solid #d0d7de; border-radius: 0.375rem; color: #0969da; display: inline-block; padding: 0.35rem 0.75rem; text-decoration: none; }
                .archive-pagination-pages [aria-current="page"],
                .archive-pagination-pages .archive-pagination-ellipsis { border: 1px solid #d0d7de; border-radius: 0.375rem; color: #1f2328; display: inline-block; padding: 0.35rem 0.75rem; }
                .archive-pagination-pages [aria-current="page"] { background: #f6f8fa; font-weight: 700; }
                .archive-pagination-ellipsis { color: #57606a; }
                .archive-pagination-prev.is-disabled,
                .archive-pagination-next.is-disabled { border-color: #eaeef2; color: #8c959f; }
              </style>
            </head>
            <body>
              <header>
                <strong>QueenZone</strong>
                <nav><a href="/">Home</a><a href="/news">News</a></nav>
              </header>
              <main>
                {{body}}
              </main>
            </body>
            </html>
            """;
    }

    private sealed record PageHeadMetadata(string? CanonicalPath = null, string? PrevPath = null, string? NextPath = null);

    private static string CultureSafeHeading(string heading) => $"<h1>{WebUtility.HtmlEncode(heading)}</h1>";

    private static string CultureSafeLink(NewsItem item) =>
        $"<a href=\"/news/{item.Id}/{Slugify(item.Title)}\">{WebUtility.HtmlEncode(item.Title)}</a>";

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDashRegex();
}