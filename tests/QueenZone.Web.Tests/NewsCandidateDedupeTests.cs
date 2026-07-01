using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsCandidateDedupeTests
{
    [Theory]
    [InlineData(
        "https://www.QueenOnline.com/news/story/?utm_source=twitter&utm_campaign=test",
        "https://www.queenonline.com/news/story")]
    [InlineData(
        "https://Example.com/path/?fbclid=abc123&ref=home",
        "https://example.com/path")]
    [InlineData(
        "https://example.com/path/",
        "https://example.com/path")]
    public void NormalizeCanonicalUrl_strips_tracking_params_and_normalizes_host(string input, string expected) =>
        Assert.Equal(expected, NewsCandidateDedupe.NormalizeCanonicalUrl(input));

    [Fact]
    public void ComputeUrlHash_is_stable_for_equivalent_urls()
    {
        var first = NewsCandidateDedupe.ComputeUrlHash(
            "https://www.queenonline.com/news/story?utm_source=twitter");
        var second = NewsCandidateDedupe.ComputeUrlHash(
            "https://www.queenonline.com/news/story?fbclid=abc");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void ComputeContentHash_normalizes_whitespace_and_case()
    {
        var first = NewsCandidateDedupe.ComputeContentHash("Queen Tour Announced", "  New dates for 2026. ");
        var second = NewsCandidateDedupe.ComputeContentHash("queen tour announced", "new dates for 2026.");

        Assert.Equal(first, second);
    }
}
