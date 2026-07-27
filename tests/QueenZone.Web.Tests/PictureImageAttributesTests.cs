using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PictureImageAttributesTests
{
    [Fact]
    public void ResolveFetchPriority_is_high_for_eager_img_hero()
    {
        Assert.Equal("high", PictureImageAttributes.ResolveFetchPriority("/design-system/assets/img-hero.jpg", lazy: false));
        Assert.Null(PictureImageAttributes.ResolveFetchPriority("/design-system/assets/img-hero.jpg", lazy: true));
        Assert.Null(PictureImageAttributes.ResolveFetchPriority("/design-system/assets/crest-white.png", lazy: false));
    }
}
