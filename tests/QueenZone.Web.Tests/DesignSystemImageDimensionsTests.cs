using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class DesignSystemImageDimensionsTests
{
    [Theory]
    [InlineData("/design-system/assets/img-hero.jpg", 2000, 1100)]
    [InlineData("/design-system/assets/crest-white.png", 447, 447)]
    [InlineData("/assets/eras/queenzone-1999.png", 787, 518)]
    [InlineData("/design-system/assets/img-hero.jpg?v=abc", 2000, 1100)]
    public void TryGet_returns_known_asset_dimensions(string src, int width, int height)
    {
        var dims = DesignSystemImageDimensions.TryGet(src);

        Assert.Equal((width, height), dims);
    }

    [Fact]
    public void TryGet_returns_null_for_unknown_paths()
    {
        Assert.Null(DesignSystemImageDimensions.TryGet("/images/unknown.jpg"));
        Assert.Null(DesignSystemImageDimensions.TryGet(""));
        Assert.Null(DesignSystemImageDimensions.TryGet(null!));
    }
}
