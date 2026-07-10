using Microsoft.Extensions.Options;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumUgcHtmlTests
{
    private static UgcHtml Create() =>
        new(Options.Create(new BlobUploadOptions()));

    [Fact]
    public void Sanitize_StripsScriptTagsBeforePersistence()
    {
        var result = Create().Sanitize("<script>alert(1)</script>");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Sanitize_KeepsForumProxyImg_StripsExternalImg()
    {
        var input =
            """<p>Hi</p><img src="/ugc/forum/a/b.webp" alt="ok"><img src="https://evil.example.com/x.jpg" alt="no">""";

        var result = Create().Sanitize(input);

        Assert.Contains("/ugc/forum/a/b.webp", result);
        Assert.DoesNotContain("evil.example.com", result);
    }

    [Fact]
    public void Sanitize_KeepsConfiguredCdnUgcImg()
    {
        var sanitizer = new UgcHtml(Options.Create(new BlobUploadOptions
        {
            PublicBaseUrl = "https://ugc.queenzone.org",
        }));

        var result = sanitizer.Sanitize(
            """<img src="https://ugc.queenzone.org/ugc-forum/editors/x.webp" alt="x">""");

        Assert.Contains("ugc.queenzone.org/ugc-forum/", result);
    }
}
