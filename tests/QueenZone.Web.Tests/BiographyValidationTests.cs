using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class BiographyValidationTests
{
    [Fact]
    public void ValidateDraft_accepts_valid_chapter()
    {
        var errors = BiographyValidation.ValidateDraft(
            new AdminBiographyDraft("1975", "Summary", "<p>Body text</p>", 3));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p><br></p>")]
    [InlineData("<p></p>")]
    public void IsEmptyBody_detects_empty_editor_markup(string? body)
    {
        Assert.True(BiographyValidation.IsEmptyBody(body));
    }

    [Fact]
    public void ValidateDraft_rejects_oversized_title()
    {
        var title = new string('a', BiographyValidation.MaxTitleLength + 1);
        var errors = BiographyValidation.ValidateDraft(
            new AdminBiographyDraft(title, "", "<p>Body</p>", 1));

        Assert.Contains(errors, error => error.Contains("Title must be"));
    }
}
