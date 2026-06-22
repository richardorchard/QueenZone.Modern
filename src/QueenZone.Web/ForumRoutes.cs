using QueenZone.Data;

namespace QueenZone.Web;

public static class ForumRoutes
{
    public static string GetCategoryPath(ForumCategoryItem category) =>
        $"/forum/{category.Id}/{NewsSlug.Slugify(category.Name)}";

    public static string FormatCount(long value) =>
        value >= 1_000_000
            ? $"{value / 1_000_000.0:0.#}M+"
            : value >= 1_000
                ? $"{value / 1_000.0:0.#}k+"
                : value.ToString("N0");
}