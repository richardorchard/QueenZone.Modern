using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsTriageResultParserTests
{
    [Fact]
    public void Parse_reads_structured_json_payload()
    {
        const string json = """
            {
              "verdict": "relevant",
              "relevance_score": 0.92,
              "confidence_score": 0.88,
              "rationale": "Official Queen tour announcement.",
              "suggested_category": "tour",
              "entities": ["Queen", "tour"],
              "review_notes": "Primary source."
            }
            """;

        var result = NewsTriageResultParser.Parse(json);

        Assert.Equal(NewsTriageVerdict.Relevant, result.Verdict);
        Assert.Equal(0.92m, result.RelevanceScore);
        Assert.Equal("tour", result.SuggestedCategory);
        Assert.Equal(["Queen", "tour"], result.Entities);
    }

    [Fact]
    public void Parse_extracts_json_from_wrapped_model_output()
    {
        const string json = """
            Here is the result:
            {
              "verdict": "not_relevant",
              "relevance_score": 0.1,
              "confidence_score": 0.95,
              "rationale": "Generic pedal review.",
              "suggested_category": "other",
              "entities": []
            }
            """;

        var result = NewsTriageResultParser.Parse(json);

        Assert.Equal(NewsTriageVerdict.NotRelevant, result.Verdict);
    }
}
