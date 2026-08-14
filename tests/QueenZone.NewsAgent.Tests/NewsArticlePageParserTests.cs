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

    [Fact]
    public void Parse_extracts_song_and_youtube_links_from_queenonline_article_markup()
    {
        var html = """
            <html>
              <head>
                <title>'I See You Now': Roger Taylor's Brand New Single &amp; Video - Out Now!</title>
                <meta name="description" content="The second single from Roger Taylor's new album." />
              </head>
              <body>
                <p>Listen to the single now @ <a href="https://rogertaylor.lnk.to/iseeyounow">https://rogertaylor.lnk.to/iseeyounow</a></p>
                <iframe src="https://www.youtube.com/embed/KZivRNcsoJw?si=test" title="YouTube video player"></iframe>
                <p><a href="https://rogertaylor.lnk.to/VIIABW">Click here</a> to pre-order the album.</p>
                <p><a href="https://www.ticketmaster.co.uk/explore/roger-taylor">Click here</a> for tickets.</p>
                <footer><a href="https://www.youtube.com/Queen">YouTube</a></footer>
              </body>
            </html>
            """;

        var parsed = NewsArticlePageParser.Parse(
            html,
            "https://www.queenonline.com/news/i-see-you-now-roger-taylors-brand-new-single-and-video-out-now");

        Assert.Collection(
            parsed.MediaLinks,
            link =>
            {
                Assert.Equal("Listen to the song", link.Label);
                Assert.Equal("https://rogertaylor.lnk.to/iseeyounow", link.Url);
            },
            link =>
            {
                Assert.Equal("Watch the video", link.Label);
                Assert.Equal("https://www.youtube.com/watch?v=KZivRNcsoJw", link.Url);
            });

        var evidenceExcerpt = NewsArticlePageParser.BuildEvidenceExcerpt(parsed);
        Assert.Contains("Direct media links supplied by the source", evidenceExcerpt, StringComparison.Ordinal);
        Assert.DoesNotContain("VIIABW", evidenceExcerpt, StringComparison.Ordinal);
        Assert.DoesNotContain("ticketmaster", evidenceExcerpt, StringComparison.OrdinalIgnoreCase);
    }
}
