namespace QueenZone.Web;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string PublicBaseUrl { get; init; } = "https://www.queenzone.org";
}