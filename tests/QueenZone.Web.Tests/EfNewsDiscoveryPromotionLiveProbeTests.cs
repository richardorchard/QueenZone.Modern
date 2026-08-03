using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for discovery → admin news promotion.
/// Self-seeds a disposable Drafted candidate (no hardcoded mirror candidate id),
/// promotes inside a rolled-back transaction, then deletes seed rows.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfNewsDiscoveryPromotionLiveProbeTests
{
    private const string ProbeEditor = "legacy-write-probe@queenzone.local";

    [Fact]
    public async Task Promotion_probe_uses_execution_strategy_and_rolls_back_when_connection_configured()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var sourceKey = $"discovery-promo-probe-{uniqueSuffix}";
        var canonicalUrl = $"https://example.com/qz-discovery-promo-probe-{uniqueSuffix}";
        var services = new ServiceCollection();
        services.AddQueenZoneLegacyData(connectionString);

        await using var provider = services.BuildServiceProvider();
        int? candidateId = null;
        int? sourceId = null;

        try
        {
            await using (var seedScope = provider.CreateAsyncScope())
            {
                var discoveryRepository = seedScope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
                var discoveredAt = DateTime.UtcNow;

                sourceId = await discoveryRepository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
                    sourceKey,
                    $"Discovery promo probe {uniqueSuffix}",
                    "https://example.com/",
                    null,
                    NewsDiscoverySourceType.AllowlistedPage,
                    NewsDiscoveryTrustTier.Primary,
                    60,
                    true,
                    null));

                candidateId = await discoveryRepository.CreateCandidateAsync(new NewsCandidateCreateRequest(
                    sourceId.Value,
                    canonicalUrl,
                    $"Discovery promo probe {uniqueSuffix}",
                    discoveredAt,
                    "Disposable mirror promotion probe excerpt.",
                    discoveredAt));

                Assert.True(await discoveryRepository.TryUpdateCandidateStatusAsync(
                    candidateId.Value,
                    new NewsCandidateStatusUpdate(
                        NewsCandidateStatus.NeedsReview,
                        RelevanceScore: 0.9m,
                        ConfidenceScore: 0.88m)));
                Assert.True(await discoveryRepository.TryUpdateCandidateStatusAsync(
                    candidateId.Value,
                    new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted)));

                await discoveryRepository.UpsertDraftAsync(
                    candidateId.Value,
                    new NewsAgentDraftUpsert(
                        $"Discovery promo probe draft {uniqueSuffix}",
                        $"discovery-promo-probe-{uniqueSuffix}",
                        "Disposable mirror promotion probe draft excerpt.",
                        "Disposable mirror promotion probe draft body.",
                        "Source: discovery promo probe",
                        "Probe notes.",
                        "Probe confidence.",
                        discoveredAt.Date,
                        null));
            }

            await using var scope = provider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();
            var discoveryRepositoryForPromote = scope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
            var adminNewsRepository = scope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
            var auditRepository = scope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

            var candidate = await discoveryRepositoryForPromote.GetCandidateByIdAsync(candidateId.Value);
            Assert.NotNull(candidate);
            Assert.True(
                NewsCandidateWorkflow.CanPromoteToArticle(candidate.Status),
                $"Seeded candidate {candidateId} is {candidate.Status}, not promotable.");

            var agentDraft = await discoveryRepositoryForPromote.GetDraftByCandidateIdAsync(candidateId.Value);
            Assert.NotNull(agentDraft);

            var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);
            var slugInUse = await adminNewsRepository.IsSlugInUseAsync(NewsSlug.Resolve(adminDraft.Title, adminDraft.Slug));
            Assert.Empty(NewsValidation.ValidateDraft(adminDraft, slugInUse));

            var originalStatus = candidate.Status;
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                var newsId = await adminNewsRepository.CreateDraftAsync(adminDraft, ProbeEditor);
                var promoted = await discoveryRepositoryForPromote.TryUpdateCandidateStatusAsync(
                    candidateId.Value,
                    new NewsCandidateStatusUpdate(
                        NewsCandidateStatus.PromotedToArticle,
                        ReviewNotes: $"Rollback-only live promotion probe for admin news draft #{newsId}.",
                        PromotedNewsId: newsId));
                Assert.True(promoted);

                await auditRepository.AppendAsync(
                    newsId,
                    "promote-from-discovery-probe",
                    ProbeEditor,
                    $"Rollback-only promotion probe for discovery candidate {candidateId}.");

                await transaction.RollbackAsync();
            });

            await using var verifyScope = provider.CreateAsyncScope();
            var verifyDiscoveryRepository = verifyScope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
            var verifyCandidate = await verifyDiscoveryRepository.GetCandidateByIdAsync(candidateId.Value);
            Assert.NotNull(verifyCandidate);
            Assert.Equal(originalStatus, verifyCandidate.Status);
            Assert.Null(verifyCandidate.PromotedNewsId);
        }
        finally
        {
            await CleanupSeedAsync(provider, candidateId, sourceId, sourceKey);
        }
    }

    private static async Task CleanupSeedAsync(
        ServiceProvider provider,
        int? candidateId,
        int? sourceId,
        string sourceKey)
    {
        await using var cleanupScope = provider.CreateAsyncScope();
        var dbContext = cleanupScope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();

        if (candidateId is int id)
        {
            await dbContext.NewsAgentDrafts
                .Where(draft => draft.CandidateId == id)
                .ExecuteDeleteAsync();
            await dbContext.NewsAiRuns
                .Where(run => run.CandidateId == id)
                .ExecuteDeleteAsync();
            await dbContext.NewsCandidateEvidence
                .Where(evidence => evidence.CandidateId == id)
                .ExecuteDeleteAsync();
            await dbContext.NewsCandidates
                .Where(candidate => candidate.Id == id)
                .ExecuteDeleteAsync();
        }

        if (sourceId is int sid)
        {
            await dbContext.NewsDiscoverySources
                .Where(source => source.Id == sid || source.Key == sourceKey)
                .ExecuteDeleteAsync();
        }
        else
        {
            await dbContext.NewsDiscoverySources
                .Where(source => source.Key == sourceKey)
                .ExecuteDeleteAsync();
        }
    }

    private static bool IsWriteProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_LEGACY_WRITE_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
