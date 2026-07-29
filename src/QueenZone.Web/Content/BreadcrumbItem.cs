namespace QueenZone.Web;

public sealed record BreadcrumbItem(string Label, string Href)
{
    public static readonly BreadcrumbItem Home = new("Home", "/");
}
