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
    var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
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
    var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
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

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task GenerateDraftAsync_force_regenerate_controls_redraft_of_drafted_candidate(bool forceRegenerate)
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions());

    var candidate = await repository.GetCandidateByIdAsync(candidateId);

    if (!forceRegenerate)
    {
      await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.GenerateDraftAsync(candidate!, new NewsDraftRunOptions()));
      return;
    }

    var result = await service.GenerateDraftAsync(
      candidate!,
      new NewsDraftRunOptions(ForceRegenerate: true));

    Assert.True(result.Succeeded);
    var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
    Assert.Equal("Queen announce 2026 tour", draft!.ProposedTitle);
  }

  [Fact]
  public async Task RunDraftGenerationAsync_skips_when_openrouter_disabled()
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson, enabled: false));

    var result = await service.RunDraftGenerationAsync(new NewsDraftRunOptions());

    Assert.Equal(0, result.CandidatesConsidered);
    Assert.Equal(0, result.DraftsCreated);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task GenerateDraftAsync_dry_run_controls_whether_draft_and_status_persist(bool dryRun)
  {
    var repository = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
    var candidateId = await NewsDiscoveryTestSeeder.SeedNeedsReviewCandidateAsync(repository);
    var service = NewsAgentTestSupport.CreateDraftGenerationService(
      repository,
      new DraftGenerationFakeAiClient(NewsAgentTestSupport.SampleDraftJson));

    var result = await service.GenerateDraftAsync(
      (await repository.GetCandidateByIdAsync(candidateId))!,
      new NewsDraftRunOptions(DryRun: dryRun));

    var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
    var candidate = await repository.GetCandidateByIdAsync(candidateId);

    if (dryRun)
    {
      Assert.Null(draft);
      Assert.Equal(NewsCandidateStatus.NeedsReview, candidate!.Status);
      return;
    }

    Assert.True(result.Succeeded);
    Assert.NotNull(draft);
    Assert.Equal(NewsCandidateStatus.Drafted, candidate!.Status);
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
