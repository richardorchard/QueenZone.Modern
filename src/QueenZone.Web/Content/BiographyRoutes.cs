using QueenZone.Data;

namespace QueenZone.Web;

public static class BiographyRoutes
{
    public const string IndexPath = "/biography";

    public static string GetChapterDetailPath(BiographyChapterItem chapter) =>
        $"/biography/{chapter.Id}/{NewsSlug.Slugify(chapter.Title)}";

    public static string GetChapterNumeral(int index, bool useRoman = true) =>
        useRoman ? ToRoman(index + 1) : (index + 1).ToString();

    public static string GetChapterNumeral(
        BiographyChapterItem chapter,
        IEnumerable<BiographyChapterItem> chapters,
        bool useRoman = true)
    {
        var readingOrder = BiographyChapterOrdering.ByDisplaySequenceAscending(chapters);
        var index = readingOrder.ToList().FindIndex(item => item.Id == chapter.Id);
        return GetChapterNumeral(Math.Max(index, 0), useRoman);
    }

    public static string GetChapterMarker(BiographyChapterItem chapter)
    {
        var marker = BiographyTitle.GetYearMarker(chapter.Title);
        return string.IsNullOrWhiteSpace(marker)
            ? $"Chapter {chapter.DisplaySequence}"
            : marker;
    }

    public static string GetReadTimeLabel(string body)
    {
        var words = CountWords(body);
        if (words <= 0)
        {
            return "Quick read";
        }

        var minutes = Math.Max(1, (int)Math.Ceiling(words / 220d));
        return minutes == 1 ? "1 min read" : $"{minutes} min read";
    }

    public static string GetIndexMetaLine(int chapterCount)
    {
        if (chapterCount <= 0)
        {
            return "No chapters available";
        }

        return chapterCount == 1
            ? "1 chapter"
            : $"{chapterCount} chapters";
    }

    private static int CountWords(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return 0;
        }

        return body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string ToRoman(int value)
    {
        if (value <= 0)
        {
            return string.Empty;
        }

        (int threshold, string numeral)[] map =
        [
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        ];

        var remaining = value;
        var builder = new System.Text.StringBuilder();
        foreach (var (threshold, numeral) in map)
        {
            while (remaining >= threshold)
            {
                builder.Append(numeral);
                remaining -= threshold;
            }
        }

        return builder.ToString();
    }
}