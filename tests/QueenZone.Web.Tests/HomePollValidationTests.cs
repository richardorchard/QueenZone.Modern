using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class HomePollValidationTests
{
    [Fact]
    public void ValidateDraft_rejects_blank_question_and_too_few_options()
    {
        var errors = HomePollValidation.ValidateDraft(new AdminHomePollDraft("  ", ["Only one"]));

        Assert.Contains(errors, error => error.Contains("Question is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("between 2 and 10", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_overlong_question_and_options()
    {
        var errors = HomePollValidation.ValidateDraft(new AdminHomePollDraft(
            new string('Q', HomePollValidation.QuestionMaxLength + 1),
            ["ok", new string('O', HomePollValidation.OptionMaxLength + 1)]));

        Assert.Contains(errors, error => error.Contains("300", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("200", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_accepts_two_trimmed_options()
    {
        var errors = HomePollValidation.ValidateDraft(new AdminHomePollDraft(
            "Best album?",
            ["  A Night at the Opera  ", "", "Sheer Heart Attack"]));

        Assert.Empty(errors);
        Assert.Equal(2, HomePollValidation.NormalizeOptions(["  A Night at the Opera  ", "", "Sheer Heart Attack"]).Count);
    }
}
