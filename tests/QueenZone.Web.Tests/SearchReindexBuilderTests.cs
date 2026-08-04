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

        var builder = new SearchReindexBuilder(indexService, newsRepository, forumRepository, articleRepository);
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
        var builder = new SearchReindexBuilder(indexService, newsRepository, forumRepository, articleRepository);

        await builder.ReindexArticlesAsync();

        var articleDocuments = store.GetAll().Where(d => d.ContentType == SiteSearchContentType.Article).ToList();
        Assert.Contains(articleDocuments, doc => doc.Title == "Published community feature");
        Assert.DoesNotContain(articleDocuments, doc => doc.Title == "Still a draft");
        _ = draft;
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
