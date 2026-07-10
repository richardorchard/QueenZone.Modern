using Microsoft.Extensions.Options;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class UgcHtmlTests
{
    private static UgcHtml Create(string? publicBaseUrl = "https://cdn.queenzone.test") =>
        new(Options.Create(new BlobUploadOptions { PublicBaseUrl = publicBaseUrl }));

    [Fact]
    public void Sanitize_strips_script_and_event_handlers()
    {
        var html = Create().Sanitize(
            """<p onclick="alert(1)">Hi<script>alert(2)</script></p><iframe src="https://evil.test"></iframe>""");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi", html);
    }

    [Fact]
    public void Sanitize_keeps_allowed_markup_and_approved_images()
    {
        var input = """
            <p><strong>Bold</strong> and <em>em</em></p>
            <ul><li>One</li></ul>
            <img src="https://cdn.queenzone.test/ugc-forum/x.jpg" alt="x">
            <img src="https://evil.test/phish.jpg" alt="no">
            """;

        var html = Create().Sanitize(input);

        Assert.Contains("<strong>", html);
        Assert.Contains("<em>", html);
        Assert.Contains("<li>", html);
        Assert.Contains("cdn.queenzone.test/ugc-forum/x.jpg", html);
        Assert.DoesNotContain("evil.test", html);
    }

    [Fact]
    public void Sanitize_allows_azure_blob_ugc_container_urls()
    {
        var html = Create(publicBaseUrl: null).Sanitize(
            """<img src="https://acct.blob.core.windows.net/ugc-articles/editors/a/b.jpg" alt="ok">""");

        Assert.Contains("blob.core.windows.net/ugc-articles/", html);
    }

    [Fact]
    public void Sanitize_rejects_azure_blob_non_ugc_paths()
    {
        var html = Create(publicBaseUrl: null).Sanitize(
            """<img src="https://acct.blob.core.windows.net/legacy-photos/a.jpg" alt="no">""");

        Assert.DoesNotContain("legacy-photos", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sanitize_empty_returns_empty(string? input) =>
        Assert.Equal(string.Empty, Create().Sanitize(input));
}
