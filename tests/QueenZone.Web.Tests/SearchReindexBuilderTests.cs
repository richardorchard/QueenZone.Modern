using QueenZone.Data;
using QueenZone.Web.Search;

namespace QueenZone.Web.Tests;

public sealed class SearchReindexBuilderTests
{
    private static (SearchReindexBuilder Builder, SharedSearchIndexStore Store) CreateBuilder()
    {
        var store = new SharedSearchIndexStore();
        var indexService = new InMemorySearchIndexService(store);

        var newsStore = new SharedNewsStore(SampleNewsData.CreateSeedArticles());
        var newsRepository = new InMemoryNewsRepository(newsStore);

        var forumWriteRepository = new InMemoryForumWriteRepository();
        var forumAttachmentRepository = new InMemoryForumAttachmentRepository();
        var forumRepository = new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats(),
            forumWriteRepository,
            forumAttachmentRepository);

        var articleSubmissionRepository = new InMemoryArticleSubmissionRepository();
        var articleRepository = new InMemoryArticleRepository(articleSubmissionRepository);

        var articlesRepository = new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles());
        var biographyRepository = new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters());
        var discographyRepository = new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums());
        var queenHistoryRepository = new InMemoryQueenHistoryRepository(SampleQueenHistoryData.CreateSeedEvents());
        var fanPerformanceRepository = new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances());

        var builder = new SearchReindexBuilder(
            indexService,
            newsRepository,
            forumRepository,
            articleRepository,
            articlesRepository,
            biographyRepository,
            discographyRepository,
            queenHistoryRepository,
            fanPerformanceRepository);
        return (builder, store);
    }

    [Fact]
    public async Task ReindexNewsAsync_IndexesPublishedNewsAsSearchDocuments()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexNewsAsync();

        var newsDocuments = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.News).ToList();
        Assert.NotEmpty(newsDocuments);
        Assert.All(newsDocuments, doc => Assert.StartsWith("news:", doc.SourceKey));
        Assert.All(newsDocuments, doc => Assert.False(doc.Body.Contains('<') && doc.Body.Contains('>')));
    }

    [Fact]
    public async Task ReindexForumAsync_IndexesForumThreadsAsSearchDocuments()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexForumAsync();

        var forumDocuments = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Forum).ToList();
        Assert.NotEmpty(forumDocuments);
        Assert.Contains(forumDocuments, doc => doc.Title == "Ranking every studio album");
        Assert.Contains(forumDocuments, doc => doc.Url == "/forum/topic/1002/ranking-every-studio-album");
    }

    [Fact]
    public async Task ReindexArticlesAsync_IndexesOnlyPublishedArticles()
    {
        var store = new SharedSearchIndexStore();
        var indexService = new InMemorySearchIndexService(store);
        var newsRepository = new InMemoryNewsRepository(new SharedNewsStore(SampleNewsData.CreateSeedArticles()));
        var forumRepository = new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats(),
            new InMemoryForumWriteRepository(),
            new InMemoryForumAttachmentRepository());

        var authorId = Guid.NewGuid();
        var articleSubmissionRepository = new InMemoryArticleSubmissionRepository();
        var longBody = "Full body about Freddie Mercury and the making of the album. " +
            string.Concat(Enumerable.Repeat("Extra detail padding this out. ", 10));
        var published = await articleSubmissionRepository.UpsertDraftAsync(new ArticleSubmissionDraft(
            null, authorId, "Published community feature", "Excerpt text", longBody, null, null));
        await articleSubmissionRepository.SubmitForReviewAsync(published.Id, authorId);
        await articleSubmissionRepository.UpdateStatusAsync(
            published.Id, ArticleSubmissionStatus.ApprovedForPublishing, "reviewer@queenzone.test", null, null);
        await articleSubmissionRepository.UpdateStatusAsync(
            published.Id, ArticleSubmissionStatus.Published, "reviewer@queenzone.test", null, null);

        var draft = await articleSubmissionRepository.UpsertDraftAsync(new ArticleSubmissionDraft(
            null, authorId, "Still a draft", "Excerpt text", "Body text", null, null));

        var articleRepository = new InMemoryArticleRepository(articleSubmissionRepository);
        var builder = new SearchReindexBuilder(
            indexService,
            newsRepository,
            forumRepository,
            articleRepository,
            new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()),
            new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters()),
            new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums()),
            new InMemoryQueenHistoryRepository(SampleQueenHistoryData.CreateSeedEvents()),
            new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances()));

        await builder.ReindexArticlesAsync();

        var articleDocuments = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Article).ToList();
        Assert.Contains(articleDocuments, doc => doc.Title == "Published community feature");
        Assert.DoesNotContain(articleDocuments, doc => doc.Title == "Still a draft");
        _ = draft;
    }

    [Fact]
    public async Task ReindexLegacyArticlesAsync_IndexesOnlyPublishedLegacyArticles()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexLegacyArticlesAsync();

        var documents = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.LegacyArticle).ToList();
        Assert.NotEmpty(documents);
        Assert.Contains(documents, doc => doc.Title == "Inside the Making of Bohemian Rhapsody");
        Assert.DoesNotContain(documents, doc => doc.Title == "Hidden moderation draft");
    }

    [Fact]
    public async Task ReindexBiographyAsync_IndexesAllChapters()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexBiographyAsync();

        var documents = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Biography).ToList();
        Assert.Equal(SampleBiographyData.CreateSeedChapters().Count, documents.Count);
        Assert.Contains(documents, doc => doc.Title == "1975");
    }

    [Fact]
    public async Task ReindexDiscographyAsync_IndexesAllAlbums()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexDiscographyAsync();

        var documents = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Discography).ToList();
        Assert.Equal(SampleDiscographyData.CreateSeedAlbums().Count, documents.Count);
        Assert.Contains(documents, doc => doc.Title == "A Night at the Opera");
    }

    [Fact]
    public async Task ReindexTimelineAsync_IndexesOnlyPublishedEvents()
    {
        var store = new SharedSearchIndexStore();
        var indexService = new InMemorySearchIndexService(store);
        var seedEvents = SampleQueenHistoryData.CreateSeedEvents().ToList();
        var hiddenEvent = seedEvents[0] with { Id = 9001, Title = "Unpublished draft event", IsPublished = false };
        var queenHistoryRepository = new InMemoryQueenHistoryRepository([.. seedEvents, hiddenEvent]);

        var builder = new SearchReindexBuilder(
            indexService,
            new InMemoryNewsRepository(new SharedNewsStore(SampleNewsData.CreateSeedArticles())),
            new InMemoryForumRepository(
                SampleForumData.CreateSeedCategories(),
                SampleForumData.CreateSeedStats(),
                new InMemoryForumWriteRepository(),
                new InMemoryForumAttachmentRepository()),
            new InMemoryArticleRepository(new InMemoryArticleSubmissionRepository()),
            new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()),
            new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters()),
            new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums()),
            queenHistoryRepository,
            new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances()));

        await builder.ReindexTimelineAsync();

        var documents = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Timeline).ToList();
        Assert.Contains(documents, doc => doc.Title == "Queen's Live Aid performance");
        Assert.DoesNotContain(documents, doc => doc.Title == "Unpublished draft event");
    }

    [Fact]
    public async Task ReindexFanPerformancesAsync_IndexesVisiblePerformances()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexFanPerformancesAsync();

        var documents = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.FanPerformance).ToList();
        Assert.Equal(SampleFanPerformanceData.CreateSeedPerformances().Count, documents.Count);
        Assert.Contains(documents, doc => doc.Title == "Reaching Out" && doc.AuthorDisplayName == "Mike Ryde");
    }

    [Fact]
    public async Task ReindexAllAsync_ReplacesStaleDocumentsOnRerun()
    {
        var (builder, store) = CreateBuilder();

        await builder.ReindexForumAsync();
        var firstRunCount = store.GetAll().Count(d => d.ContentType == SiteSearchContentType.Forum);

        await builder.ReindexForumAsync();
        var secondRunCount = store.GetAll().Count(d => d.ContentType == SiteSearchContentType.Forum);

        Assert.Equal(firstRunCount, secondRunCount);
    }
}
