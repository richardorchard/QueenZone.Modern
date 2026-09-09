namespace QueenZone.Web;

/// <summary>
/// Builds consistent Admin > Section > Page breadcrumb trails for pages under
/// <c>/admin</c>, using the shared <see cref="BreadcrumbItem"/> model and
/// <c>_Breadcrumbs</c> partial already used across the public site.
/// </summary>
public static class AdminBreadcrumbs
{
    public static readonly BreadcrumbItem Root = new("Admin", "/admin");

    /// <summary>Admin > Section (Section renders as the current, unlinked page).</summary>
    public static IReadOnlyList<BreadcrumbItem> Section(string sectionLabel, string sectionHref)
        => [Root, new BreadcrumbItem(sectionLabel, sectionHref)];

    /// <summary>Admin > Section > Page (Page renders as the current, unlinked page).</summary>
    public static IReadOnlyList<BreadcrumbItem> Page(string sectionLabel, string sectionHref, string pageLabel, string pageHref = "#")
        => [Root, new BreadcrumbItem(sectionLabel, sectionHref), new BreadcrumbItem(pageLabel, pageHref)];

    /// <summary>Admin > Section > Intermediate > Page, for pages nested two levels deep.</summary>
    public static IReadOnlyList<BreadcrumbItem> Page(string sectionLabel, string sectionHref, string intermediateLabel, string intermediateHref, string pageLabel, string pageHref = "#")
        => [Root, new BreadcrumbItem(sectionLabel, sectionHref), new BreadcrumbItem(intermediateLabel, intermediateHref), new BreadcrumbItem(pageLabel, pageHref)];
}
