using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDraftResultParserTests
{
  [Fact]
  public void Parse_reads_structured_draft_payload()
  {
    var result = NewsDraftResultParser.Parse(NewsAgentTestSupport.SampleDraftJson);

    Assert.Equal("Queen announce 2026 tour", result.Title);
    Assert.Equal("queen-announce-2026-tour", result.Slug);
    Assert.Contains("Queen Online", result.SourceNames);
    Assert.False(result.SecondarySourceWarning);
  }

  [Fact]
  public void Parse_throws_for_missing_title()
  {
    const string json = """
      {
        "excerpt": "Excerpt",
        "body": "Body"
      }
      """;

    Assert.Throws<InvalidOperationException>(() => NewsDraftResultParser.Parse(json));
  }
}
