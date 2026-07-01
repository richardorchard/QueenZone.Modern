using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDraftGenerationOptionsTests
{
  [Fact]
  public void Validate_rejects_invalid_per_run_limit()
  {
    var options = new NewsDraftGenerationOptions { PerRunCandidateLimit = 0 };

    var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());

    Assert.Contains("PerRunCandidateLimit", exception.Message, StringComparison.Ordinal);
  }
}
