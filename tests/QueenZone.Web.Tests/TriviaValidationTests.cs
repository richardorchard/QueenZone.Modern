using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class TriviaValidationTests
{
    [Fact]
    public void ValidateDraft_accepts_a_well_formed_draft()
    {
        var errors = TriviaValidation.ValidateDraft(
            new AdminTriviaDraft("Freddie was born in Zanzibar.", true, "Band", TriviaDifficulty.Easy, "Bio"));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraft_accepts_optional_fields_when_omitted()
    {
        var errors = TriviaValidation.ValidateDraft(new AdminTriviaDraft("A single fact.", true));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraft_requires_fact_text()
    {
        var errors = TriviaValidation.ValidateDraft(new AdminTriviaDraft("   ", true));

        Assert.Contains(errors, error => error.Contains("Fact text is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_fact_text_over_the_max_length()
    {
        var errors = TriviaValidation.ValidateDraft(
            new AdminTriviaDraft(new string('a', TriviaValidation.MaxTextLength + 1), true));

        Assert.Contains(errors, error => error.Contains($"{TriviaValidation.MaxTextLength} characters", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_category_over_the_max_length()
    {
        var errors = TriviaValidation.ValidateDraft(
            new AdminTriviaDraft("A fact.", true, new string('c', TriviaValidation.MaxCategoryLength + 1)));

        Assert.Contains(errors, error => error.Contains("Category must be", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_unknown_difficulty()
    {
        var errors = TriviaValidation.ValidateDraft(
            new AdminTriviaDraft("A fact.", true, "Band", "expert"));

        Assert.Contains(errors, error => error.Contains("easy, medium, or hard", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_source_over_the_max_length()
    {
        var errors = TriviaValidation.ValidateDraft(
            new AdminTriviaDraft("A fact.", true, null, null, new string('s', TriviaValidation.MaxSourceLength + 1)));

        Assert.Contains(errors, error => error.Contains("Source must be", StringComparison.Ordinal));
    }
}
