using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PublicContentMapperTests
{
    [Fact]
    public void ToNewsArchiveItem_MapsPresentationFieldsAndDetailPath()
    {
        var item = new NewsItem(
            42,
            "Queen tour dates",
            "Excerpt",
            "Body",
            new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            "https://example.com/source",
            true,
            "custom-slug");

        var view = PublicContentMapper.ToNewsArchiveItem(item);

        Assert.Equal(42, view.Id);
        Assert.Equal("Queen tour dates", view.Title);
        Assert.Equal("Excerpt", view.Excerpt);
        Assert.Equal(item.PublishedAt, view.PublishedAt);
        Assert.Equal("/news/42/custom-slug", view.DetailPath);
    }

    [Fact]
    public void ToNewsDetailItem_FromAdminArticle_UsesResolvedSlug()
    {
        var article = new AdminNewsArticle(
            7,
            "Preview title",
            "preview-title",
            "Excerpt",
            "Body text",
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "https://example.com",
            false,
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            "editor@example.com");

        var view = PublicContentMapper.ToNewsDetailItem(article);

        Assert.Equal(7, view.Id);
        Assert.Equal("Body text", view.Body);
        Assert.Equal("https://example.com", view.SourceUrl);
        Assert.Equal("/news/7/preview-title", view.DetailPath);
    }

    [Fact]
    public void ToArticleArchiveItem_IncludesCategoryAndDetailPath()
    {
        var item = new ArticleItem(
            101,
            "Studio essay",
            "Excerpt",
            "Body",
            new DateTime(2023, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            null,
            "Recording",
            true);

        var view = PublicContentMapper.ToArticleArchiveItem(item);

        Assert.Equal("Recording", view.CategoryName);
        Assert.Equal("/articles/101/studio-essay", view.DetailPath);
    }

    [Fact]
    public void ToForumThreadHeader_TrimsNamesAndBuildsPaths()
    {
        var header = new ForumTopicHeader(1002, " Ranking every studio album ", 3, " The Music ");

        var view = PublicContentMapper.ToForumThreadHeader(header);

        Assert.Equal("Ranking every studio album", view.Title);
        Assert.Equal("The Music", view.ForumName);
        Assert.Equal("/forum/3/the-music", view.CategoryPath);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", view.DetailPath);
    }

    [Fact]
    public void ToForumPostViewModel_MapsAttachments()
    {
        var post = new ForumPostItem(
            9,
            "Post body",
            new DateTime(2020, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            "brian",
            "sig",
            12,
            new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            [new ForumPostAttachment("scan.jpg", 2048)]);

        var view = PublicContentMapper.ToForumPostViewModel(post);

        Assert.Equal("brian", view.AuthorUsername);
        Assert.Single(view.Attachments);
        Assert.Equal("scan.jpg", view.Attachments[0].FileName);
        Assert.Equal("https://pictures.queenzone.org/attachments/scan.jpg", view.Attachments[0].Url);
        Assert.Equal("JPG", view.Attachments[0].Extension);
        Assert.Equal("2.0 KB", view.Attachments[0].FormattedSize);
    }

    [Fact]
    public void ToForumIndexStats_AggregatesCategoryPostCounts()
    {
        var categories = new[]
        {
            new ForumCategoryItem(1, "A", null, 10, null, null, 1),
            new ForumCategoryItem(2, "B", null, 5, null, null, 2)
        };

        var stats = PublicContentMapper.ToForumIndexStats(categories, threadCount: 4);

        Assert.Equal(2, stats.ForumCount);
        Assert.Equal(4, stats.ThreadCount);
        Assert.Equal(15, stats.PostCount);
    }
}
