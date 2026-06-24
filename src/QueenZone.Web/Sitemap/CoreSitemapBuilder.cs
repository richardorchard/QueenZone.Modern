using QueenZone.Data;

namespace QueenZone.Web.Sitemap;

public sealed class CoreSitemapBuilder(
    INewsRepository newsRepository,
    IArticlesRepository articlesRepository,
    IForumRepository forumRepository)
{
    public async Task<IReadOnlyList<SitemapEntry>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry> { new("/") };

        await AddNewsEntriesAsync(entries, cancellationToken);
        await AddArticleEntriesAsync(entries, cancellationToken);
        await AddForumEntriesAsync(entries, cancellationToken);

        return entries;
    }

    private async Task AddNewsEntriesAsync(List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        var publishedCount = await newsRepository.GetPublishedCountAsync(cancellationToken);
        var totalPages = NewsRoutes.GetArchiveTotalPages(publishedCount);
        for (var page = 1; page <= totalPages; page++)
        {
            entries.Add(new(NewsRoutes.GetArchiveCanonicalPath(page)));
        }

        var newsItems = await newsRepository.GetPublishedSitemapEntriesAsync(cancellationToken);
        foreach (var item in newsItems)
        {
            entries.Add(new(
                NewsArticleContent.GetDetailCanonicalPath(item.Id, item.Title, item.Slug),
                item.PublishedAt));
        }
    }

    private async Task AddArticleEntriesAsync(List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        var publishedCount = await articlesRepository.GetPublishedCountAsync(cancellationToken);
        var totalPages = ArticlesRoutes.GetArchiveTotalPages(publishedCount);
        for (var page = 1; page <= totalPages; page++)
        {
            entries.Add(new(ArticlesRoutes.GetArchiveCanonicalPath(page)));
        }

        var articleItems = await articlesRepository.GetPublishedSitemapEntriesAsync(cancellationToken);
        foreach (var item in articleItems)
        {
            entries.Add(new(
                ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title),
                item.PublishedAt));
        }
    }

    private async Task AddForumEntriesAsync(List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        entries.Add(new("/forum"));

        var categories = await forumRepository.GetCategoriesAsync(cancellationToken);
        foreach (var category in categories)
        {
            entries.Add(new(
                ForumRoutes.GetCategoryCanonicalPath(category),
                category.LastActivityAt));
        }
    }
}