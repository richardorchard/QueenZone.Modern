using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Search;

/// <summary>
/// Repeatable batch job that rebuilds <c>SearchDocument</c> rows for one or more content types by
/// reading the same public repositories the archive pages already use — so the visibility rules
/// applied there (published/display flags) are exactly the rules search inherits. Lives in
/// QueenZone.Web (not QueenZone.Data) because it needs the same canonical-URL route helpers
/// (<see cref="NewsRoutes"/>, <see cref="ForumRoutes"/>, <see cref="ArticlesRoutes"/>) that
/// <see cref="Sitemap.ForumSitemapBuilder"/> uses for the same reason.
/// </summary>
/// <remarks>
/// Covers News, Forum threads, Community Articles, legacy Articles, Biography, Discography,
/// Timeline, Fan Performances, and Freddie Tribute. Photography is not yet wired in — it needs
/// the same three-step treatment (paginate the existing public repository, map to
/// <see cref="SearchDocumentEntity"/>, call <see cref="ISearchIndexService.ReplaceContentTypeAsync"/>)
/// and can be added as an additional <c>Reindex*Async</c> method following the pattern below.
/// </remarks>
public sealed class SearchReindexBuilder(
    ISearchIndexService searchIndexService,
    INewsRepository newsRepository,
    IForumRepository forumRepository,
    IArticleRepository articleRepository,
    IArticlesRepository articlesRepository,
    IBiographyRepository biographyRepository,
    IDiscographyRepository discographyRepository,
    IQueenHistoryRepository queenHistoryRepository,
    IFanPerformanceRepository fanPerformanceRepository,
    IFreddieTributeRepository freddieTributeRepository)
{
    private const int BatchSize = 200;

    /// <param name="onContentTypeStarted">
    /// Optional progress callback with the <see cref="SiteSearchContentType"/> key about to be replaced.
    /// Used by the admin background job UI; ignored by startup seed and unit tests.
    /// </param>
    public async Task ReindexAllAsync(
        CancellationToken cancellationToken = default,
        Action<string>? onContentTypeStarted = null)
    {
        await RunContentTypeAsync(SiteSearchContentType.News, () => ReindexNewsAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Forum, () => ReindexForumAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Article, () => ReindexArticlesAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.LegacyArticle, () => ReindexLegacyArticlesAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Biography, () => ReindexBiographyAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Discography, () => ReindexDiscographyAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Timeline, () => ReindexTimelineAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.FanPerformance, () => ReindexFanPerformancesAsync(cancellationToken), onContentTypeStarted);
        await RunContentTypeAsync(SiteSearchContentType.Tribute, () => ReindexFreddieTributeAsync(cancellationToken), onContentTypeStarted);
    }

    private static async Task RunContentTypeAsync(
        string contentType,
        Func<Task> reindex,
        Action<string>? onContentTypeStarted)
    {
        onContentTypeStarted?.Invoke(contentType);
        await reindex();
    }

    public async Task ReindexNewsAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await newsRepository.GetPublishedCountAsync(cancellationToken);
        var documents = new List<SearchDocumentEntity>();

        for (var page = 1; (page - 1) * BatchSize < totalCount; page++)
        {
            var items = await newsRepository.GetArchivePageAsync(page, BatchSize, cancellationToken);
            documents.AddRange(items.Select(MapNews));
        }

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.News, documents, cancellationToken);
    }

    public async Task ReindexForumAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await forumRepository.GetTopicSitemapCountAsync(cancellationToken);
        var documents = new List<SearchDocumentEntity>();

        for (var offset = 0; offset < totalCount; offset += BatchSize)
        {
            var items = await forumRepository.GetTopicSitemapPageAsync(offset, BatchSize, cancellationToken);
            documents.AddRange(items.Select(MapForumThread));
        }

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Forum, documents, cancellationToken);
    }

    public async Task ReindexArticlesAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await articleRepository.GetCountAsync(ct: cancellationToken);
        var documents = new List<SearchDocumentEntity>();

        for (var page = 1; (page - 1) * BatchSize < totalCount; page++)
        {
            var items = await articleRepository.GetPageAsync(page, BatchSize, ct: cancellationToken);
            documents.AddRange(items.Select(MapArticle));
        }

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Article, documents, cancellationToken);
    }

    public async Task ReindexLegacyArticlesAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await articlesRepository.GetPublishedCountAsync(cancellationToken);
        var documents = new List<SearchDocumentEntity>();

        for (var page = 1; (page - 1) * BatchSize < totalCount; page++)
        {
            var items = await articlesRepository.GetArchivePageAsync(page, BatchSize, cancellationToken);
            documents.AddRange(items.Select(MapLegacyArticle));
        }

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.LegacyArticle, documents, cancellationToken);
    }

    public async Task ReindexBiographyAsync(CancellationToken cancellationToken = default)
    {
        var chapters = await biographyRepository.GetChaptersAsync(cancellationToken);
        var documents = chapters.Select(MapBiographyChapter).ToList();

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Biography, documents, cancellationToken);
    }

    public async Task ReindexDiscographyAsync(CancellationToken cancellationToken = default)
    {
        var albums = await discographyRepository.GetAlbumsAsync(cancellationToken);
        var documents = albums.Select(MapAlbum).ToList();

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Discography, documents, cancellationToken);
    }

    public async Task ReindexTimelineAsync(CancellationToken cancellationToken = default)
    {
        var events = await queenHistoryRepository.GetAllPublishedAsync(cancellationToken);
        var documents = events.Select(MapTimelineEvent).ToList();

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Timeline, documents, cancellationToken);
    }

    public async Task ReindexFanPerformancesAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await fanPerformanceRepository.GetVisibleCountAsync(cancellationToken);
        var documents = new List<SearchDocumentEntity>();

        for (var page = 1; (page - 1) * BatchSize < totalCount; page++)
        {
            var items = await fanPerformanceRepository.GetPageAsync(page, BatchSize, cancellationToken);
            documents.AddRange(items.Select(MapFanPerformance));
        }

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.FanPerformance, documents, cancellationToken);
    }

    public async Task ReindexFreddieTributeAsync(CancellationToken cancellationToken = default)
    {
        var documents = new List<SearchDocumentEntity>();
        var page = 1;
        int totalCount;

        do
        {
            var result = await freddieTributeRepository.GetPageAsync(page, BatchSize, cancellationToken);
            totalCount = result.TotalCount;
            documents.AddRange(result.Items.Select(MapFreddieTribute));
            page++;
        }
        while ((page - 1) * BatchSize < totalCount);

        await searchIndexService.ReplaceContentTypeAsync(SiteSearchContentType.Tribute, documents, cancellationToken);
    }

    private static SearchDocumentEntity MapLegacyArticle(ArticleItem item)
    {
        var plainBody = SearchDocumentText.ToPlainText(item.Body);
        return new SearchDocumentEntity
        {
            SourceKey = $"legacy-article:{item.Id}",
            ContentType = SiteSearchContentType.LegacyArticle,
            Title = item.Title,
            Body = plainBody,
            Summary = SearchDocumentText.Summarize(
                string.IsNullOrWhiteSpace(item.Excerpt) ? plainBody : item.Excerpt),
            Url = ArticlesRoutes.GetArticleDetailPath(item.Id, item.Title),
            PublishedAt = item.PublishedAt,
            Category = item.CategoryName,
        };
    }

    private static SearchDocumentEntity MapBiographyChapter(BiographyChapterItem chapter)
    {
        var plainBody = SearchDocumentText.ToPlainText(chapter.Body);
        return new SearchDocumentEntity
        {
            SourceKey = $"biography:{chapter.Id}",
            ContentType = SiteSearchContentType.Biography,
            Title = chapter.Title,
            Body = plainBody,
            Summary = SearchDocumentText.Summarize(
                string.IsNullOrWhiteSpace(chapter.Summary) ? plainBody : chapter.Summary),
            Url = BiographyRoutes.GetChapterDetailPath(chapter),
            PublishedAt = chapter.CreatedAt == DateTime.MinValue ? null : chapter.CreatedAt,
        };
    }

    private static SearchDocumentEntity MapAlbum(AlbumSummary album) =>
        new()
        {
            SourceKey = $"discography:{album.AlbumId}",
            ContentType = SiteSearchContentType.Discography,
            Title = album.Name,
            Body = album.Name,
            Summary = album.Name,
            Url = DiscographyRoutes.GetAlbumPath(album),
            PublishedAt = album.ReleaseYear.HasValue
                ? new DateTimeOffset(album.ReleaseYear.Value, 1, 1, 0, 0, 0, TimeSpan.Zero)
                : null,
        };

    private static SearchDocumentEntity MapTimelineEvent(QueenHistoryEvent historyEvent) =>
        new()
        {
            SourceKey = $"timeline:{historyEvent.Id}",
            ContentType = SiteSearchContentType.Timeline,
            Title = historyEvent.Title,
            Body = historyEvent.Summary,
            Summary = SearchDocumentText.Summarize(historyEvent.Summary),
            Url = $"/timeline#event-{historyEvent.Id}",
            PublishedAt = historyEvent.EventDate,
            Category = historyEvent.Category.ToString(),
        };

    private static SearchDocumentEntity MapFanPerformance(FanPerformance performance) =>
        new()
        {
            SourceKey = $"fan-performance:{performance.Id}",
            ContentType = SiteSearchContentType.FanPerformance,
            Title = performance.Title,
            Body = performance.Description,
            Summary = SearchDocumentText.Summarize(performance.Description),
            Url = FanPerformanceRoutes.GetIndexPath(),
            PublishedAt = performance.DateAdded,
            AuthorDisplayName = performance.PerformedBy,
        };

    private static SearchDocumentEntity MapFreddieTribute(FreddieTribute tribute) =>
        new()
        {
            SourceKey = $"tribute:{tribute.Id}",
            ContentType = SiteSearchContentType.Tribute,
            Title = tribute.Name,
            Body = tribute.Thought,
            Summary = SearchDocumentText.Summarize(tribute.Thought),
            Url = $"/freddie-mercury-tribute#tribute-{tribute.Id}",
            AuthorDisplayName = tribute.Name,
        };

    /// <summary>
    /// Maps one published news item to its <see cref="SearchDocumentEntity"/> shape. Exposed so
    /// admin publish/unpublish handlers can upsert a single document immediately rather than
    /// waiting for the next full reindex — see <c>Admin/News/Action.cshtml.cs</c>.
    /// </summary>
    public static SearchDocumentEntity MapNews(NewsItem item)
    {
        var plainBody = SearchDocumentText.ToPlainText(item.Body);
        return new SearchDocumentEntity
        {
            SourceKey = $"news:{item.Id}",
            ContentType = SiteSearchContentType.News,
            Title = item.Title,
            Body = plainBody,
            Summary = SearchDocumentText.Summarize(
                string.IsNullOrWhiteSpace(item.Excerpt) ? plainBody : item.Excerpt),
            Url = NewsRoutes.GetNewsDetailPath(item),
            PublishedAt = item.PublishedAt,
        };
    }

    private static SearchDocumentEntity MapForumThread(ForumTopicSitemapItem item) =>
        new()
        {
            SourceKey = $"forum-thread:{item.TopicId}",
            ContentType = SiteSearchContentType.Forum,
            Title = item.Title,
            // Sitemap projection only carries the thread title, not post bodies — full post
            // text would need a dedicated bulk read from ModernForumPost; title-only indexing
            // is a deliberate scope trade-off for the initial reindex source (see remarks above).
            Body = item.Title,
            Summary = item.Title,
            Url = ForumRoutes.GetTopicCanonicalPath(item.TopicId, item.Title),
            PublishedAt = item.LastActivityAt,
        };

    /// <summary>
    /// Maps one published community article to its <see cref="SearchDocumentEntity"/> shape.
    /// Exposed for the same immediate-upsert reason as <see cref="MapNews"/> — see
    /// <c>Admin/Articles/Action.cshtml.cs</c>.
    /// </summary>
    public static SearchDocumentEntity MapArticle(PublishedArticleSubmission item)
    {
        var plainBody = SearchDocumentText.ToPlainText(item.Body);
        return new SearchDocumentEntity
        {
            SourceKey = $"article:{item.Slug}",
            ContentType = SiteSearchContentType.Article,
            Title = item.Title,
            Body = plainBody,
            Summary = SearchDocumentText.Summarize(
                string.IsNullOrWhiteSpace(item.Excerpt) ? plainBody : item.Excerpt),
            Url = ArticlesRoutes.GetCommunityArticleDetailPath(item.Slug),
            PublishedAt = item.PublishedAt,
            AuthorDisplayName = item.AuthorDisplayName,
        };
    }
}
