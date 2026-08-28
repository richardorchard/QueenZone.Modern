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
}
