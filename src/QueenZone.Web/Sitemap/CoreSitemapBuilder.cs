using QueenZone.Data;

namespace QueenZone.Web.Sitemap;

public sealed class CoreSitemapBuilder(
    INewsRepository newsRepository,
    IArticlesRepository articlesRepository,
    IBiographyRepository biographyRepository,
    IForumRepository forumRepository,
    IPhotoRepository photoRepository)
{
    public async Task<IReadOnlyList<SitemapEntry>> BuildAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<SitemapEntry> { new("/") };

        await AddNewsEntriesAsync(entries, cancellationToken);
        await AddArticleEntriesAsync(entries, cancellationToken);
        await AddBiographyEntriesAsync(entries, cancellationToken);
        await AddForumEntriesAsync(entries, cancellationToken);
        await AddPhotographyEntriesAsync(entries, cancellationToken);

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

    private async Task AddPhotographyEntriesAsync(List<SitemapEntry> entries, CancellationToken cancellationToken)
    {
        entries.Add(new(PhotoRoutes.GetCategoriesPath()));

        var categories = await photoRepository.GetCategoriesAsync(cancellationToken);
        foreach (var category in categories)
        {
            var photos = await photoRepository.GetCategoryAllAsync(category.CatId, cancellationToken);
            var latestPhotoAt = photos.Count > 0
                ? photos.Max(photo => photo.DateTime)
                : (DateTime?)null;

            entries.Add(new(PhotoRoutes.GetCategoryPath(category.Slug), latestPhotoAt));

            var totalPages = PhotoRoutes.GetCategoryTotalPages(category.ImageCount);
            for (var page = 2; page <= totalPages; page++)
            {
                entries.Add(new(PhotoRoutes.GetCategoryPagePath(category.Slug, page), latestPhotoAt));
            }

            foreach (var photo in photos)
            {
                entries.Add(new(
                    PhotoRoutes.GetDetailPath(category.Slug, photo.PicId),
                    photo.DateTime));
            }
        }
    }
}