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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ReturnsNullWhenNoAttachment(string? attachment)
    {
        var result = ForumPostAttachment.Parse(attachment, "1024");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_ReturnsSingleAttachmentWithParsedSize()
    {
        var result = ForumPostAttachment.Parse("setlist.jpg", "284712");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("setlist.jpg", result[0].FileName);
        Assert.Equal(284_712L, result[0].FileSizeBytes);
    }

    [Fact]
    public void Parse_HandlesNullFilesizeAsNullBytes()
    {
        var result = ForumPostAttachment.Parse("setlist.jpg", null);
        Assert.NotNull(result);
        Assert.Null(result[0].FileSizeBytes);
    }

    [Fact]
    public void Parse_TrimsWhitespaceFromFilename()
    {
        var result = ForumPostAttachment.Parse("  setlist.jpg  ", "0");
        Assert.NotNull(result);
        Assert.Equal("setlist.jpg", result[0].FileName);
    }
}
