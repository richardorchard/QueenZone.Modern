using Microsoft.Extensions.Options;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumUgcHtmlTests
{
    [Fact]
    public void Sanitize_StripsScriptTagsBeforePersistence()
    {
        var sanitizer = new UgcHtml(Options.Create(new BlobUploadOptions()));

        var result = sanitizer.Sanitize("<script>alert(1)</script>");

        Assert.Equal(string.Empty, result);
    }
}
