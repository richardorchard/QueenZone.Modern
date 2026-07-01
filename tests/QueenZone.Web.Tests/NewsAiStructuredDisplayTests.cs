using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsAiStructuredDisplayTests
{
    [Fact]
    public void TryReadTriageSummary_parses_rationale_and_entities()
    {
        const string json = """
            {
              "verdict": "relevant",
              "rationale": "Mentions Queen tour dates.",
              "entities": ["Queen", "Brian May"],
              "review_notes": "High confidence official source."
            }
            """;

        var parsed = NewsAiStructuredDisplay.TryReadTriageSummary(json, out var summary);

        Assert.True(parsed);
        Assert.NotNull(summary);
        Assert.Equal("relevant", summary.Verdict);
        Assert.Equal("Mentions Queen tour dates.", summary.Rationale);
        Assert.Equal(["Queen", "Brian May"], summary.Entities);
        Assert.Equal("High confidence official source.", summary.ReviewNotes);
    }

    [Fact]
    public void TryReadTriageSummary_returns_false_for_empty_json()
    {
        var parsed = NewsAiStructuredDisplay.TryReadTriageSummary(null, out var summary);

        Assert.False(parsed);
        Assert.Null(summary);
    }
}
