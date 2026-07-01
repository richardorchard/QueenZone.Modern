using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDraftGenerationServiceTests
{
  [Fact]
  public async Task GenerateDraftAsync_creates_unpublished_draft_and_marks_candidate_drafted()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    var result = await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions());

    Assert.True(result.Succeeded);
    Assert.NotNull(result.DraftId);

    var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
    var candidate = await repository.GetCandidateByIdAsync(candidateId);
    Assert.NotNull(draft);
    Assert.Equal("Queen announce 2026 tour", draft.ProposedTitle);
    Assert.Contains("Queen Online", draft.AttributionText, StringComparison.Ordinal);
    Assert.Equal(NewsCandidateStatus.Drafted, candidate!.Status);
    Assert.Null(candidate.PromotedNewsId);
  }

  [Fact]
  public async Task GenerateDraftAsync_skips_when_draft_already_exists()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await SeedNeedsReviewCandidateAsync(repository);
    await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
      "Existing draft",
      "existing-draft",
      "Existing excerpt",
      "Existing body",
      "Source: Queen Online",
      "Existing source notes",
      "Existing confidence notes",
      null,
      null));
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    var result = await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions());

    Assert.False(result.Succeeded);
    Assert.NotNull(result.DraftId);
  }

  [Fact]
  public async Task GenerateDraftAsync_regenerates_when_drafted_and_forced()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions());

    var result = await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions(ForceRegenerate: true));

    Assert.True(result.Succeeded);
    var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
    Assert.Equal("Queen announce 2026 tour", draft!.ProposedTitle);
  }

  [Fact]
  public async Task RunDraftGenerationAsync_skips_when_openrouter_disabled()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    await SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson, enabled: false));

    var result = await service.RunDraftGenerationAsync(new NewsDraftRunOptions());

    Assert.Equal(0, result.CandidatesConsidered);
    Assert.Equal(0, result.DraftsCreated);
  }

  [Fact]
  public async Task GenerateDraftAsync_dry_run_does_not_persist_draft_or_status()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions(DryRun: true));

    Assert.Null(await repository.GetDraftByCandidateIdAsync(candidateId));
    Assert.Equal(NewsCandidateStatus.NeedsReview, (await repository.GetCandidateByIdAsync(candidateId))!.Status);
  }

  private static async Task<int> SeedNeedsReviewCandidateAsync(INewsDiscoveryRepository repository)
  {
    var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
      "queen-online",
      "Queen Online",
      "https://www.queenonline.com/",
      "https://www.queenonline.com/feed/",
      NewsDiscoverySourceType.Rss,
      NewsDiscoveryTrustTier.Primary,
      60,
      true,
      null));
    var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
      sourceId,
      "https://www.queenonline.com/news/tour-2026",
      "Queen announce 2026 tour",
      DateTime.UtcNow,
      "Official dates announced.",
      DateTime.UtcNow));
    await repository.TryUpdateCandidateStatusAsync(
      candidateId,
      new NewsCandidateStatusUpdate(
        NewsCandidateStatus.NeedsReview,
        ConfidenceScore: 0.90m,
        RelevanceScore: 0.92m));

    return candidateId;
  }

  private sealed class DraftGenerationFakeAiClient(string content, bool enabled = true) : INewsAiClient
  {
    public bool IsEnabled { get; } = enabled;

    public Task<NewsAiChatCompletion> CompleteChatAsync(
      NewsAiChatRequest request,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(new NewsAiChatCompletion(
        content,
        "openai/gpt-4.1-mini",
        120,
        240,
        0.0025m,
        DryRun: false));
  }
}
