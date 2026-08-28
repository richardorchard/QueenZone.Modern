using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsArticleImageTests
{
    [Fact]
    public void ResolveImageUrl_returns_null_when_unset()
    {
        Assert.Null(NewsArticleImage.ResolveImageUrl(null, null));
        Assert.Null(NewsArticleImage.ResolveThumbnailUrl("  ", null));
    }

    [Fact]
    public void ResolveImageUrl_uses_articles_proxy_and_thumb_query()
    {
        Assert.Equal(
            "/ugc/articles/editors/me/hero.webp",
            NewsArticleImage.ResolveImageUrl("editors/me/hero.webp", null));
        Assert.Equal(
            "/ugc/articles/editors/me/hero.webp?size=thumb",
            NewsArticleImage.ResolveThumbnailUrl("editors/me/hero.webp", null));
    }

    [Fact]
    public void ResolveImageUrl_does_not_treat_gallery_prefix_as_articles_blob()
    {
        Assert.True(NewsArticleImage.IsGalleryReference("gallery:3120"));
        Assert.Null(NewsArticleImage.ArticlesBlobName("gallery:3120"));
        Assert.Null(NewsArticleImage.ResolveImageUrl("gallery:3120", null));
        Assert.Null(NewsArticleImage.ResolveImageUrl(null, 3120));
    }

    [Fact]
    public void ToGalleryReference_formats_typed_pic_id()
    {
        Assert.Equal("gallery:101", NewsArticleImage.ToGalleryReference(101));
        Assert.Equal(101, NewsArticleImage.TryParseGalleryPicId("gallery:101"));
        Assert.Equal(101, NewsArticleImage.TryParseGalleryPicId("GALLERY:101"));
        Assert.Null(NewsArticleImage.TryParseGalleryPicId("gallery:"));
        Assert.Null(NewsArticleImage.TryParseGalleryPicId("editors/me/hero.webp"));

        var draft = NewsArticleImage.WithGalleryPick(
            new AdminNewsDraft("Title", null, "Excerpt", "Body", DateTime.UtcNow.Date, null, "editors/me/old.webp"),
            101);
        Assert.Equal("gallery:101", draft.ImageBlobKey);
        Assert.Equal(101, draft.ImageGalleryPicId);
    }

    [Fact]
    public void ResolveDisplayUrl_returns_placeholder_when_unset()
    {
        Assert.Equal(NewsArticleImage.PlaceholderPath, NewsArticleImage.ResolveDisplayUrl(null, null));
        Assert.Equal(NewsArticleImage.PlaceholderPath, NewsArticleImage.ResolveDisplayThumbnailUrl("  ", null));
        Assert.False(NewsArticleImage.HasResolvedImage(null, null));
    }

    [Fact]
    public void ResolveDisplayUrl_returns_placeholder_for_gallery_until_resolved()
    {
        Assert.Equal(NewsArticleImage.PlaceholderPath, NewsArticleImage.ResolveDisplayUrl("gallery:3120", null));
        Assert.Equal(NewsArticleImage.PlaceholderPath, NewsArticleImage.ResolveDisplayUrl(null, 3120));
        Assert.False(NewsArticleImage.HasResolvedImage("gallery:3120", null));
    }

    [Fact]
    public void ResolveDisplayUrl_keeps_articles_proxy_when_set()
    {
        Assert.Equal(
            "/ugc/articles/editors/me/hero.webp",
            NewsArticleImage.ResolveDisplayUrl("editors/me/hero.webp", null));
        Assert.Equal(
            "/ugc/articles/editors/me/hero.webp?size=thumb",
            NewsArticleImage.ResolveDisplayThumbnailUrl("editors/me/hero.webp", null));
        Assert.True(NewsArticleImage.HasResolvedImage("editors/me/hero.webp", null));
    }
}
