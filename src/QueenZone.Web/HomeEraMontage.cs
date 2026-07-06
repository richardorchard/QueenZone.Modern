using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QueenZone.Web;

public static class HomeEraMontage
{
    private static readonly (string Year, string ImagePath, string Label, string Glow)[] Slides =
    [
        ("1999", "/assets/eras/queenzone-1999.png", "The Queen Internet Zone", "#c81e2e"),
        ("2000", "/assets/eras/queenzone-2000.png", "Queen Internet Zone", "#3c4a5a"),
        ("2002", "/assets/eras/queenzone-2002.png", "www.queenzone.com", "#9c1414"),
        ("2004", "/assets/eras/queenzone-2004.png", "Queenzone.com", "#1668ad"),
        ("2020", "/assets/eras/queenzone-2020.png", "QUEENZONE.COM", "#8b95a1"),
    ];

    public static IReadOnlyList<HomeEraSlide> GetSlides(
        IFileVersionProvider fileVersionProvider,
        PathString requestPathBase) =>
        Slides.Select(slide => new HomeEraSlide(
            slide.Year,
            VersionedStaticPath.Apply(fileVersionProvider, requestPathBase, slide.ImagePath),
            slide.Label,
            slide.Glow)).ToArray();
}

public sealed record HomeEraSlide(string Year, string Img, string Label, string Glow);
