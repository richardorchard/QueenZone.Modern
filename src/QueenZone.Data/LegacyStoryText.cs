using System.Net;
using System.Text.RegularExpressions;

namespace QueenZone.Data;

public static partial class LegacyStoryText
{
    public static string GetExcerpt(string? body, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var plain = HtmlTagRegex().Replace(body, " ");
        plain = WebUtility.HtmlDecode(plain);
        plain = WhitespaceRegex().Replace(plain, " ").Trim();
        if (plain.Length <= maxLength)
        {
            return plain;
        }

        return plain[..maxLength].TrimEnd() + "…";
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}