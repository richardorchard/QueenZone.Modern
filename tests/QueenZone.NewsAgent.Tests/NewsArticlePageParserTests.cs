using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

public sealed class NewsArticlePageParserTests
{
    [Fact]
    public void Parse_prefers_richer_open_graph_title_with_attributable_quote()
    {
        var html = """
            <html>
              <head>
                <title>Tony Iommi on Brian May's guest spot | MusicRadar</title>
                <meta property="og:title" content="&ldquo;We just hit it off&rdquo;: Tony Iommi reveals Brian May's guest performance">
                <meta name="description" content="Iommi discusses Brian May's appearance on his new solo album.">
              </head>
            </html>
            """;

        var parsed = NewsArticlePageParser.Parse(html, "https://www.musicradar.com/example");

        Assert.Equal(
            "\u201cWe just hit it off\u201d: Tony Iommi reveals Brian May's guest performance",
            parsed.Title);
        Assert.Contains("Brian May", parsed.Excerpt, StringComparison.Ordinal);
    }
}
