using System.Text.RegularExpressions;

namespace QueenZone.Web.Tests;

internal static partial class TestHtmlAssertions
{
    public static void AssertPageTitle(string html, string expectedTitle)
    {
        var titleMatch = TitleRegex().Match(html);
        Assert.True(titleMatch.Success, "Expected a page title but none was rendered.");

        var actualTitle = System.Net.WebUtility.HtmlDecode(titleMatch.Groups["title"].Value).Trim();
        Assert.Equal(expectedTitle, actualTitle);
    }

    [GeneratedRegex("<title>(?<title>.*?)</title>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();
}
