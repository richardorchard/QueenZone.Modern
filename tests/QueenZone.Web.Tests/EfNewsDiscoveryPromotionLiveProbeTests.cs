using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfNewsDiscoveryPromotionLiveProbeTests
{
    [Fact]
    public async Task Promotion_probe_uses_execution_strategy_and_rolls_back_when_connection_configured()
    {
        if (!IsWriteProbeEnabled(out var connectionString))
        {
            return;
        }

        var candidateId = GetCandidateId();
        var services = new ServiceCollection();
        services.AddQueenZoneLegacyData(connectionString);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();
        var discoveryRepository = scope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
        var adminNewsRepository = scope.ServiceProvider.GetRequiredService<IAdminNewsRepository>();
        var auditRepository = scope.ServiceProvider.GetRequiredService<INewsAuditRepository>();

        var candidate = await discoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.True(
            NewsCandidateWorkflow.CanPromoteToArticle(candidate.Status),
            $"Candidate {candidateId} is {candidate.Status}, not promotable.");

        var agentDraft = await discoveryRepository.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(agentDraft);

        var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);
        var slugInUse = await adminNewsRepository.IsSlugInUseAsync(NewsSlug.Resolve(adminDraft.Title, adminDraft.Slug));
        Assert.Empty(NewsValidation.ValidateDraft(adminDraft, slugInUse));

        var originalStatus = candidate.Status;
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var newsId = await adminNewsRepository.CreateDraftAsync(adminDraft, "legacy-write-probe@queenzone.local");
            var promoted = await discoveryRepository.TryUpdateCandidateStatusAsync(
                candidateId,
                new NewsCandidateStatusUpdate(
                    NewsCandidateStatus.PromotedToArticle,
                    ReviewNotes: $"Rollback-only live promotion probe for admin news draft #{newsId}.",
                    PromotedNewsId: newsId));
            Assert.True(promoted);

            await auditRepository.AppendAsync(
                newsId,
                "promote-from-discovery-probe",
                "legacy-write-probe@queenzone.local",
                $"Rollback-only promotion probe for discovery candidate {candidateId}.");

            await transaction.RollbackAsync();
        });

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDiscoveryRepository = verifyScope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
        var verifyCandidate = await verifyDiscoveryRepository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(verifyCandidate);
        Assert.Equal(originalStatus, verifyCandidate.Status);
        Assert.Null(verifyCandidate.PromotedNewsId);
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

    private static int GetCandidateId() =>
        int.TryParse(
            Environment.GetEnvironmentVariable("NEWS_DISCOVERY_PROMOTION_PROBE_CANDIDATE_ID"),
            out var candidateId)
            ? candidateId
            : 237;
}
