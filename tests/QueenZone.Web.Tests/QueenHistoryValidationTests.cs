using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class QueenHistoryValidationTests
{
    [Fact]
    public void ValidateDraft_accepts_a_well_formed_draft()
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft());

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraft_requires_title()
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { Title = "   " });

        Assert.Contains(errors, error => error.Contains("Title is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_title_over_the_max_length()
    {
        var errors = QueenHistoryValidation.ValidateDraft(
            ValidDraft() with { Title = new string('a', QueenHistoryValidation.MaxTitleLength + 1) });

        Assert.Contains(errors, error => error.Contains("200 characters", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_requires_summary()
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { Summary = "  " });

        Assert.Contains(errors, error => error.Contains("Summary is required", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_rejects_summary_over_the_max_length()
    {
        var errors = QueenHistoryValidation.ValidateDraft(
            ValidDraft() with { Summary = new string('a', QueenHistoryValidation.MaxSummaryLength + 1) });

        Assert.Contains(errors, error => error.Contains("1000 characters", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_requires_event_date()
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { EventDate = default });

        Assert.Contains(errors, error => error.Contains("Event date is required", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ValidateDraft_rejects_importance_outside_range(int importance)
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { Importance = importance });

        Assert.Contains(errors, error => error.Contains("between 0 and 100", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void ValidateDraft_accepts_importance_at_bounds(int importance)
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { Importance = importance });

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateDraft_rejects_source_url_over_the_max_length()
    {
        var errors = QueenHistoryValidation.ValidateDraft(
            ValidDraft() with { SourceUrl = "https://example.com/" + new string('a', QueenHistoryValidation.MaxSourceUrlLength) });

        Assert.Contains(errors, error => error.Contains("2000 characters", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("/relative")]
    [InlineData("ftp://example.com/file")]
    public void ValidateDraft_rejects_non_absolute_http_source_url(string sourceUrl)
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { SourceUrl = sourceUrl });

        Assert.Contains(errors, error => error.Contains("absolute http or https URL", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateDraft_accepts_empty_source_url()
    {
        var errors = QueenHistoryValidation.ValidateDraft(ValidDraft() with { SourceUrl = null });

        Assert.Empty(errors);
    }

    private static AdminQueenHistoryDraft ValidDraft() =>
        new(
            "Live Aid",
            "Queen perform at Wembley.",
            new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc),
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Concert,
            100,
            "https://example.com/live-aid",
            true);
}
