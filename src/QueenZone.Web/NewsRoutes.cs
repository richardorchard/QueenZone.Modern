using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using QueenZone.Data;

namespace QueenZone.Web;

public static partial class NewsRoutes
{
    public static IEndpointRouteBuilder MapNewsRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", async (INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            var latest = await newsRepository.GetLatestAsync(5, cancellationToken);
            return Results.Content(RenderPage("QueenZone", RenderNewsList("Latest news", latest)), "text/html; charset=utf-8");
        });

        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        endpoints.MapGet("/news", async (INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            var archive = await newsRepository.GetArchivePageAsync(1, 20, cancellationToken);
            return Results.Content(RenderPage("QueenZone news", RenderNewsList("News archive", archive)), "text/html; charset=utf-8");
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

        endpoints.MapGet("/news.aspx", () => Results.Redirect("/news", permanent: true));

        endpoints.MapGet("/process/news_view.aspx", async (HttpRequest request, INewsRepository newsRepository, CancellationToken cancellationToken) =>
        {
            if (!int.TryParse(request.Query["news_id"], out var id))
            {
                return Results.Redirect("/news", permanent: true);
            }

            var item = await newsRepository.GetByIdAsync(id, cancellationToken);
            return item is null
                ? Results.Redirect("/news", permanent: true)
                : Results.Redirect($"/news/{item.Id}/{Slugify(item.Title)}", permanent: true);
        });

        return endpoints;
    }

    public static string Slugify(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var replaced = NonAlphaNumericRegex().Replace(lower, "-");
        return DuplicateDashRegex().Replace(replaced, "-").Trim('-');
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

    private static string RenderPage(string title, string body) =>
        $$"""
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{{WebUtility.HtmlEncode(title)}}</title>
          <style>
            body { font-family: Arial, sans-serif; line-height: 1.5; margin: 0 auto; max-width: 880px; padding: 2rem; }
            header { border-bottom: 1px solid #d0d7de; margin-bottom: 2rem; padding-bottom: 1rem; }
            nav a { margin-right: 1rem; }
            .news-list { padding-left: 1.5rem; }
            .news-list li { margin-bottom: 1.5rem; }
            .date, time { color: #57606a; }
            .lede { font-size: 1.15rem; }
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

    private static string CultureSafeHeading(string heading) => $"<h1>{WebUtility.HtmlEncode(heading)}</h1>";

    private static string CultureSafeLink(NewsItem item) =>
        $"<a href=\"/news/{item.Id}/{Slugify(item.Title)}\">{WebUtility.HtmlEncode(item.Title)}</a>";

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateDashRegex();
}
