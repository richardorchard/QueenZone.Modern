using System.Text.RegularExpressions;

namespace QueenZone.Web.Tests;

public sealed partial class LayoutSkipLinkTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public LayoutSkipLinkTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/about")]
    [InlineData("/forum")]
    public async Task PublicPages_RenderSkipLinkBeforeHeaderAndMainLandmark(string path)
    {
        var html = await factory.CreateClient().GetStringAsync(path);

        var skipLink = SkipLinkRegex().Match(html);
        Assert.True(skipLink.Success, $"Expected a skip-to-content link on {path}.");

        var headerIndex = html.IndexOf("<header", StringComparison.OrdinalIgnoreCase);
        Assert.True(headerIndex >= 0, $"Expected a header landmark on {path}.");
        Assert.True(
            skipLink.Index < headerIndex,
            "Skip link must be the first focusable control, before the site header.");

        Assert.Matches(@"<main\b[^>]*\bid=""main-content""", html);
        Assert.Matches(@"<main\b[^>]*\btabindex=""-1""", html);
    }

    [GeneratedRegex(
        @"<a\b[^>]*\bhref=""#main-content""[^>]*>\s*Skip to content\s*</a>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex SkipLinkRegex();
}
