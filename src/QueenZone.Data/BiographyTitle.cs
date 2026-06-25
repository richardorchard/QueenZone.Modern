using System.Text.RegularExpressions;

namespace QueenZone.Data;

public static partial class BiographyTitle
{
    public static string GetYearMarker(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var trimmed = title.Trim();
        var rangeMatch = YearRangeRegex().Match(trimmed);
        if (rangeMatch.Success)
        {
            return $"{rangeMatch.Groups[1].Value}–{rangeMatch.Groups[2].Value}";
        }

        var singleYearMatch = SingleYearRegex().Match(trimmed);
        if (singleYearMatch.Success)
        {
            return singleYearMatch.Groups[1].Value;
        }

        return trimmed;
    }

    [GeneratedRegex(@"^(\d{4})\s*[-–—]\s*(\d{4})$")]
    private static partial Regex YearRangeRegex();

    [GeneratedRegex(@"^(\d{4})$")]
    private static partial Regex SingleYearRegex();
}