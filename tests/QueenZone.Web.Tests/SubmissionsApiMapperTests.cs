using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class SubmissionsApiMapperTests
{
    [Fact]
    public void ToStatus_UsesWebsiteBadgeLabelAndTone()
    {
        var status = SubmissionsApiMapper.ToStatus(PhotoSubmissionStatus.UnderReview);

        Assert.Equal(PhotoSubmissionStatus.UnderReview, status.Status);
        Assert.Equal("Under review", status.StatusLabel);
        Assert.Equal("review", status.StatusTone);
    }

    [Fact]
    public void ToPhoto_PrefersRejectionReasonAndBuildsUgcThumbnailPath()
    {
        var item = SamplePhoto(
            PhotoSubmissionStatus.Rejected,
            reviewNotes: "Looks off",
            rejectionReason: "Too dark");

        var dto = SubmissionsApiMapper.ToPhoto(item);

        Assert.Equal(item.Id, dto.Id);
        Assert.Equal("Live at Wembley", dto.Title);
        Assert.Equal("Too dark", dto.Notes);
        Assert.Equal(
            UgcProxyPaths.GetPath(BlobUploadContainers.Photos, "members/a/thumb.webp"),
            dto.ThumbnailPath);
        Assert.Equal("danger", dto.Status.StatusTone);
        Assert.Equal("Rejected", dto.Status.StatusLabel);
    }

    [Fact]
    public void ToPhoto_OmitsThumbnailPathWhenBlobPathIsEmpty()
    {
        var item = SamplePhoto(
            PhotoSubmissionStatus.Pending,
            reviewNotes: null,
            rejectionReason: null) with
        { ThumbnailBlobPath = " " };

        Assert.Null(SubmissionsApiMapper.ToPhoto(item).ThumbnailPath);
    }

    [Fact]
    public void ToPhoto_UsesReviewNotesWhenNotRejected()
    {
        var item = SamplePhoto(
            PhotoSubmissionStatus.NeedsInfo,
            reviewNotes: "Please add a year",
            rejectionReason: null);

        Assert.Equal("Please add a year", SubmissionsApiMapper.ToPhoto(item).Notes);
    }

    [Fact]
    public void ToNews_TruncatesUrlAndKeepsReviewNotes()
    {
        var url = "https://example.com/" + new string('x', 100);
        var suggestion = new NewsSuggestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            url,
            "hash",
            "Headline",
            "member notes",
            NewsSuggestionStatus.UnderReview,
            DateTimeOffset.UtcNow,
            null,
            "admin@test.local",
            "Checking sources",
            null,
            null,
            "Fan",
            "fan@example.com");

        var dto = SubmissionsApiMapper.ToNews(suggestion, publishedPath: null);

        Assert.Equal(url, dto.Url);
        Assert.Equal(SubmissionStatusPresentation.TruncateUrl(url), dto.TruncatedUrl);
        Assert.Equal("Checking sources", dto.Notes);
        Assert.Null(dto.PublishedPath);
        Assert.Equal("Under review", dto.Status.StatusLabel);
    }

    [Fact]
    public async Task ResolvePublishedNewsPathAsync_OnlyReturnsPublishedPromotedArticles()
    {
        var news = new FixedNewsRepository(
        [
            new NewsItem(1003, "QueenZone modernisation begins", "excerpt", "body", DateTime.UtcNow, null, true, "modernisation"),
            new NewsItem(9001, "Hidden", "excerpt", "body", DateTime.UtcNow, null, false),
        ]);

        var promoted = new NewsSuggestion(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.com/story",
            "hash",
            "Headline",
            null,
            NewsSuggestionStatus.Promoted,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "admin@test.local",
            null,
            1003,
            null,
            null,
            null);
        var unpublished = promoted with { PromotedNewsId = 9001 };
        var pending = promoted with { Status = NewsSuggestionStatus.Pending, PromotedNewsId = null };

        Assert.Equal(
            NewsRoutes.GetNewsDetailPath(1003, "QueenZone modernisation begins", "modernisation"),
            await SubmissionsApiMapper.ResolvePublishedNewsPathAsync(promoted, news, CancellationToken.None));
        Assert.Null(await SubmissionsApiMapper.ResolvePublishedNewsPathAsync(unpublished, news, CancellationToken.None));
        Assert.Null(await SubmissionsApiMapper.ResolvePublishedNewsPathAsync(pending, news, CancellationToken.None));
    }

    [Fact]
    public void ToArticle_Draft_CanContinueEditing()
    {
        var item = SampleArticle(ArticleSubmissionStatus.Draft, submittedAt: null);

        var dto = SubmissionsApiMapper.ToArticle(item);

        Assert.True(dto.CanContinueEditing);
        Assert.Equal($"/submit/article/{item.Id:D}", dto.EditPath);
        Assert.Null(dto.PublishedPath);
        Assert.Null(dto.SubmittedAt);
        Assert.Equal("Draft", dto.Status.StatusLabel);
        Assert.Equal("pending", dto.Status.StatusTone);
    }

    [Fact]
    public void ToArticle_RequiresRevision_PrefersReviewNotes()
    {
        var item = SampleArticle(
            ArticleSubmissionStatus.RequiresRevision,
            submittedAt: DateTimeOffset.UtcNow,
            reviewNotes: "Please cite sources",
            rejectionReason: "Not used");

        var dto = SubmissionsApiMapper.ToArticle(item);

        Assert.True(dto.CanContinueEditing);
        Assert.Equal("Please cite sources", dto.Notes);
        Assert.Equal("Requires revision", dto.Status.StatusLabel);
        Assert.Equal("attention", dto.Status.StatusTone);
    }

    [Fact]
    public void ToArticle_Published_ExposesCommunityPath()
    {
        var item = SampleArticle(
            ArticleSubmissionStatus.Published,
            submittedAt: DateTimeOffset.UtcNow,
            slug: "fan-essay");

        var dto = SubmissionsApiMapper.ToArticle(item);

        Assert.False(dto.CanContinueEditing);
        Assert.Null(dto.EditPath);
        Assert.Equal(ArticlesRoutes.GetCommunityArticleDetailPath("fan-essay"), dto.PublishedPath);
        Assert.Equal("success", dto.Status.StatusTone);
    }

    [Fact]
    public void ToArticle_Rejected_UsesRejectionReason()
    {
        var item = SampleArticle(
            ArticleSubmissionStatus.Rejected,
            submittedAt: DateTimeOffset.UtcNow,
            reviewNotes: null,
            rejectionReason: "Off topic");

        Assert.Equal("Off topic", SubmissionsApiMapper.ToArticle(item).Notes);
        Assert.Equal("danger", SubmissionsApiMapper.ToArticle(item).Status.StatusTone);
    }

    private static PhotoSubmission SamplePhoto(
        string status,
        string? reviewNotes,
        string? rejectionReason) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Live at Wembley",
            null,
            "Queen",
            null,
            null,
            null,
            "members/a/original.jpg",
            "members/a/display.webp",
            "members/a/thumb.webp",
            "shot.jpg",
            1024,
            "image/jpeg",
            800,
            600,
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "admin@test.local",
            reviewNotes,
            rejectionReason);

    private static ArticleSubmission SampleArticle(
        string status,
        DateTimeOffset? submittedAt,
        string? reviewNotes = null,
        string? rejectionReason = null,
        string slug = "draft-article") =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fan essay",
            slug,
            "Excerpt",
            new string('a', 320),
            null,
            null,
            status,
            submittedAt,
            status == ArticleSubmissionStatus.Published ? DateTimeOffset.UtcNow : null,
            "admin@test.local",
            reviewNotes,
            rejectionReason,
            "Fan",
            "fan@example.com");
}
