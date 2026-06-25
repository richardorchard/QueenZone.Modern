using QueenZone.Data;

namespace QueenZone.Web.Sitemap;

public sealed class CoreSitemapBuilder(
    INewsRepository newsRepository,
    IArticlesRepository articlesRepository,
    IBiographyRepository biographyRepository,
    IForumRepository forumRepository)
{
    public async Task<IReadOnlyList<SitemapEntry>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry> { new("/") };

        await AddNewsEntriesAsync(entries, cancellationToken);
        await AddArticleEntriesAsync(entries, cancellationToken);
        await AddBiographyEntriesAsync(entries, cancellationToken);
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

        for (var page = 1; page <= totalPages; page++)
        {
            var archivePage = await newsRepository.GetArchivePageAsync(
                page,
                NewsRoutes.ArchivePageSize,
                cancellationToken);
            foreach (var item in archivePage)
            {
                entries.Add(new(
                    NewsArticleContent.GetDetailCanonicalPath(item.Id, item.Title, item.Slug),
                    item.PublishedAt));
            }
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

        for (var page = 1; page <= totalPages; page++)
        {
            var archivePage = await articlesRepository.GetArchivePageAsync(
                page,
                ArticlesRoutes.ArchivePageSize,
                cancellationToken);
            foreach (var item in archivePage)
            {
                entries.Add(new(
                    ArticlesRoutes.GetArticleDetailPath(item),
                    item.PublishedAt));
            }
        }
    }

    private async Task AddBiographyEntriesAsync(List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        entries.Add(new(BiographyRoutes.IndexPath));

        var chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        foreach (var chapter in chapters)
        {
            entries.Add(new(
                BiographyRoutes.GetChapterDetailPath(chapter),
                chapter.CreatedAt == DateTime.MinValue ? null : chapter.CreatedAt));
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