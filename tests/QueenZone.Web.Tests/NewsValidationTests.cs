using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsValidationTests
{
    [Fact]
    public void ValidateDraftAcceptsValidPlainTextArticle()
    {
        var draft = new AdminNewsDraft(
            "Valid title",
            null,
            "Valid excerpt",
            "Plain text body",
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            "https://example.com/source");

        var errors = NewsValidation.ValidateDraft(draft, slugInUse: false);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraftRejectsHtmlBodyAndUnsafeSourceUrl()
    {
        var draft = new AdminNewsDraft(
            "Title",
            null,
            "Excerpt",
            "<p>Not plain text</p>",
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            "javascript:alert(1)");

        var errors = NewsValidation.ValidateDraft(draft, slugInUse: false);

        Assert.Contains("Article body must be plain text.", errors);
        Assert.Contains("Source URL must be a safe http or https link.", errors);
    }

    [Fact]
    public void ValidateDraftRejectsOverlongTitle()
    {
        var draft = new AdminNewsDraft(
            new string('x', NewsValidation.MaxTitleLength + 1),
            null,
            "Excerpt",
            "Body",
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            null);

        var errors = NewsValidation.ValidateDraft(draft, slugInUse: false);

        Assert.Contains($"Title must be {NewsValidation.MaxTitleLength} characters or fewer.", errors);
    }

    [Fact]
    public void ValidateDraftRejectsOverlongSourceUrl()
    {
        var draft = new AdminNewsDraft(
            "Title",
            null,
            "Excerpt",
            "Body",
            new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            "https://example.com/" + new string('a', NewsValidation.MaxSourceUrlLength));

        var errors = NewsValidation.ValidateDraft(draft, slugInUse: false);

        Assert.Contains($"Source URL must be {NewsValidation.MaxSourceUrlLength} characters or fewer.", errors);
    }

    [Fact]
    public void NewsSlugResolveUsesOverrideWhenProvided()
    {
        Assert.Equal("custom-slug", NewsSlug.Resolve("Any title", "Custom Slug!"));
        Assert.Equal("generated-title", NewsSlug.Resolve("Generated Title", null));
    }

    [Fact]
    public void NewsSlugResolveCapsGeneratedSlugAtDatabaseLimit()
    {
        var slug = NewsSlug.Resolve("Generated title", new string('a', NewsSlug.MaxLength + 25));

        Assert.Equal(NewsSlug.MaxLength, slug.Length);
        Assert.All(slug, character => Assert.Equal('a', character));
    }
}
