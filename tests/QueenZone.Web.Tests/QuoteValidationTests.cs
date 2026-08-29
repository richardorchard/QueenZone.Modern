using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class QuoteValidationTests
{
    [Fact]
    public void ValidateDraft_accepts_a_well_formed_draft()
    {
        var errors = QuoteValidation.ValidateDraft(new AdminQuoteDraft("A kind of magic.", "Freddie Mercury", true));

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraft_requires_quote_text()
    {
        var errors = QuoteValidation.ValidateDraft(new AdminQuoteDraft("   ", "Freddie Mercury", true));

        Assert.Contains(errors, error => error.Contains("Quote text is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_requires_who_said_it()
    {
        var errors = QuoteValidation.ValidateDraft(new AdminQuoteDraft("A kind of magic.", "  ", true));

        Assert.Contains(errors, error => error.Contains("Who said it is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_quote_text_over_the_max_length()
    {
        var errors = QuoteValidation.ValidateDraft(
            new AdminQuoteDraft(new string('a', QuoteValidation.MaxTextLength + 1), "Freddie Mercury", true));

        Assert.Contains(errors, error => error.Contains($"{QuoteValidation.MaxTextLength} characters", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_who_said_it_over_the_max_length()
    {
        var errors = QuoteValidation.ValidateDraft(
            new AdminQuoteDraft("A kind of magic.", new string('a', QuoteValidation.MaxWhoSaidLength + 1), true));

        Assert.Contains(errors, error => error.Contains("50 characters", StringComparison.Ordinal));
    }
}
