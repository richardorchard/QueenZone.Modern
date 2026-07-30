using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Public RSS 2.0 feeds for published news and articles (archive + community).
/// Canonical URLs: <see cref="NewsRoutes.FeedPath"/>, <see cref="ArticlesRoutes.FeedPath"/>.
/// </summary>
public static class PublicRssFeedEndpoints
{
    public static void MapPublicRssFeedEndpoints(this WebApplication app)
    {
        app.MapGet(NewsRoutes.FeedPath, HandleNewsFeedAsync)
            .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);

        app.MapGet(ArticlesRoutes.FeedPath, HandleArticlesFeedAsync)
            .CacheOutput(PublicOutputCachePolicies.PublicSitemaps);
    }

    /// <summary>Legacy registration name used by older call sites / tests.</summary>
    public static void MapArticlesFeedEndpoint(this WebApplication app) =>
        MapPublicRssFeedEndpoints(app);

    private static async Task<IResult> HandleNewsFeedAsync(
        INewsRepository newsRepository,
        IOptions<SiteOptions> siteOptions,
        CancellationToken cancellationToken)
    {
        var baseUrl = NormalizeBaseUrl(siteOptions.Value.PublicBaseUrl);
        var items = await newsRepository.GetLatestAsync(RssFeedBuilder.DefaultItemLimit, cancellationToken);
        var feedItems = items.Select(item => new RssFeedBuilder.Item(
            item.Title,
            baseUrl + NewsArticleContent.GetDetailCanonicalPath(item.Id, item.Title, item.Slug),
            string.IsNullOrWhiteSpace(item.Excerpt) ? null : item.Excerpt,
            item.PublishedAt));

        var xml = RssFeedBuilder.Build(
            channelTitle: "QueenZone News",
            channelLink: baseUrl + "/news",
            channelDescription: "The latest Queen news and stories from QueenZone.",
            selfAbsoluteUrl: baseUrl + NewsRoutes.FeedPath,
            items: feedItems);

        return Results.Content(xml, "application/rss+xml; charset=utf-8");
    }

    private static async Task<IResult> HandleArticlesFeedAsync(
        IArticlesRepository articlesRepository,
        IArticleRepository articleRepository,
        IOptions<SiteOptions> siteOptions,
        CancellationToken cancellationToken)
    {
        var baseUrl = NormalizeBaseUrl(siteOptions.Value.PublicBaseUrl);
        var feedItems = new List<RssFeedBuilder.Item>();

        var archive = await articlesRepository.GetLatestAsync(
            RssFeedBuilder.DefaultItemLimit,
            cancellationToken);
        foreach (var item in archive)
        {
            feedItems.Add(new RssFeedBuilder.Item(
                item.Title,
                baseUrl + ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title),
                string.IsNullOrWhiteSpace(item.Excerpt) ? null : item.Excerpt,
                item.PublishedAt));
        }

        try
        {
            var community = await articleRepository.GetSitemapEntriesAsync(cancellationToken);
            foreach (var article in community)
            {
                feedItems.Add(new RssFeedBuilder.Item(
                    article.Title,
                    baseUrl + ArticlesRoutes.GetCommunityArticleDetailPath(article.Slug),
                    string.IsNullOrWhiteSpace(article.Excerpt) ? null : article.Excerpt,
                    article.PublishedAt.UtcDateTime));
            }
        }
        catch (SqlException)
        {
            // Community table may be unavailable before migration; archive feed still works.
        }

        var ordered = feedItems
            .OrderByDescending(item => item.PublishedAtUtc)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(RssFeedBuilder.DefaultItemLimit);

        var xml = RssFeedBuilder.Build(
            channelTitle: "QueenZone Articles",
            channelLink: baseUrl + "/articles",
            channelDescription: "In-depth Queen articles and community features from QueenZone.",
            selfAbsoluteUrl: baseUrl + ArticlesRoutes.FeedPath,
            items: ordered);

        return Results.Content(xml, "application/rss+xml; charset=utf-8");
    }

    private static string NormalizeBaseUrl(string? publicBaseUrl) =>
        (publicBaseUrl ?? string.Empty).TrimEnd('/');
}
