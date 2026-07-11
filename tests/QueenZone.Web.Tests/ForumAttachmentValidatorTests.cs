using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ForumAttachmentValidatorTests
{
    private readonly ForumAttachmentValidator validator = new(
        Options.Create(new ForumAttachmentOptions
        {
            MaxFilesPerPost = 5,
            MaxBytesPerFile = 20 * 1024 * 1024,
            MaxTotalBytesPerPost = 50 * 1024 * 1024,
        }));

    [Fact]
    public void Validate_RejectsMoreThanMaxFiles()
    {
        var files = Enumerable.Range(0, 6)
            .Select(i => CreateFile($"f{i}.pdf", 100, "application/pdf"))
            .ToList();

        var result = validator.Validate(files);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("at most 5", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.AcceptedFiles);
    }

    [Fact]
    public void Validate_RejectsOversizedIndividualFile()
    {
        var files = new List<IFormFile>
        {
            CreateFile("big.pdf", (20 * 1024 * 1024) + 1, "application/pdf"),
        };

        var result = validator.Validate(files);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("too large", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsWhenTotalExceedsLimit()
    {
        var files = new List<IFormFile>
        {
            CreateFile("a.pdf", 20 * 1024 * 1024, "application/pdf"),
            CreateFile("b.pdf", 20 * 1024 * 1024, "application/pdf"),
            CreateFile("c.pdf", 15 * 1024 * 1024, "application/pdf"),
        };

        var result = validator.Validate(files);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("exceeds", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(2, result.AcceptedFiles.Count);
    }

    [Fact]
    public void Validate_RejectsDisallowedMimeTypes()
    {
        var files = new List<IFormFile>
        {
            CreateFile("payload.exe", 1024, "application/x-msdownload"),
        };

        var result = validator.Validate(files);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("not allowed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AcceptsAllowedPdf()
    {
        var files = new List<IFormFile>
        {
            CreateFile("notes.pdf", 2048, "application/pdf"),
        };

        var result = validator.Validate(files);

        Assert.True(result.IsValid);
        Assert.Single(result.AcceptedFiles);
    }

    private static IFormFile CreateFile(string name, long length, string contentType)
    {
        var stream = new MemoryStream(new byte[Math.Max(length, 0)]);
        if (length > 0 && stream.Length < length)
        {
            stream.SetLength(length);
        }

        return new FormFile(stream, 0, length, "Attachments", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
