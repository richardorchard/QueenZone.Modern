using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumPostAttachmentTests
{
    [Fact]
    public void Url_IsCorrectBlobStoragePath()
    {
        var attachment = new ForumPostAttachment("setlist-scan.jpg", 100_000);
        Assert.Equal("https://pictures.queenzone.org/attachments/setlist-scan.jpg", attachment.Url);
    }

    [Theory]
    [InlineData("photo.JPG", "JPG")]
    [InlineData("document.pdf", "PDF")]
    [InlineData("archive.zip", "ZIP")]
    [InlineData("noextension", "")]
    public void Extension_ReturnsUppercaseExtensionWithoutDot(string fileName, string expected)
    {
        var attachment = new ForumPostAttachment(fileName, null);
        Assert.Equal(expected, attachment.Extension);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(0L, "")]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(2_560L, "2.5 KB")]
    [InlineData(1_048_576L, "1.0 MB")]
    [InlineData(2_621_440L, "2.5 MB")]
    public void FormattedSize_FormatsCorrectly(long? bytes, string expected)
    {
        var attachment = new ForumPostAttachment("file.jpg", bytes);
        Assert.Equal(expected, attachment.FormattedSize);
    }
}
