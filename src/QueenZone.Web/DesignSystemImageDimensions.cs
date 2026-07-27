namespace QueenZone.Web;

/// <summary>
/// Intrinsic pixel sizes for known design-system / static assets so
/// <c>_PictureImage</c> can emit width/height and avoid CLS without every call site
/// hard-coding dimensions.
/// </summary>
public static class DesignSystemImageDimensions
{
    private static readonly Dictionary<string, (int Width, int Height)> Known =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["/design-system/assets/img-hero.jpg"] = (2000, 1100),
            ["/design-system/assets/img-stage.jpg"] = (1200, 800),
            ["/design-system/assets/img-portrait.jpg"] = (900, 1200),
            ["/design-system/assets/img-crowd.jpg"] = (1400, 900),
            ["/design-system/assets/img-studio.jpg"] = (1000, 1000),
            ["/design-system/assets/crest-white.png"] = (447, 447),
            ["/design-system/assets/crest-black.png"] = (447, 447),
            ["/assets/eras/queenzone-1999.png"] = (787, 518),
            ["/assets/eras/queenzone-2000.png"] = (774, 609),
            ["/assets/eras/queenzone-2002.png"] = (692, 615),
            ["/assets/eras/queenzone-2004.png"] = (793, 609),
            ["/assets/eras/queenzone-2020.png"] = (960, 774),
        };

    public static (int Width, int Height)? TryGet(string src)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return null;
        }

        var path = src.AsSpan().Trim();
        var query = path.IndexOf('?');
        if (query >= 0)
        {
            path = path[..query];
        }

        return Known.TryGetValue(path.ToString(), out var dims) ? dims : null;
    }
}
