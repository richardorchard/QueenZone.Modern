using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminNewsWriteServicePromotionTests
{
    [Fact]
    public async Task PromoteDiscoveryCandidateAsync_CreatesDraftUpdatesCandidateAndAudits()
    {
        var newsStore = new SharedNewsStore();
        var admin = new InMemoryAdminNewsRepository(newsStore);
        var discovery = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var audit = new InMemoryNewsAuditRepository(newsStore);
        var candidateId = await NewsDiscoveryTestSeeder.SeedDraftedCandidateAsync(discovery);
        var candidate = await discovery.GetCandidateByIdAsync(candidateId);
        var agentDraft = await discovery.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.NotNull(agentDraft);
        var adminDraft = NewsDiscoveryPromoteDraft.Build(agentDraft, candidate);
        var service = CreateService(admin, discovery, audit);

        var newsId = await service.PromoteDiscoveryCandidateAsync(
            candidate,
            agentDraft,
            adminDraft,
            "editor@test.local");

        var article = await admin.GetByIdAsync(newsId);
        Assert.NotNull(article);
        Assert.False(article!.IsPublished);
        Assert.Equal("Discovery draft title", article.Title);

        var updated = await discovery.GetCandidateByIdAsync(candidateId);
        Assert.Equal(NewsCandidateStatus.PromotedToArticle, updated!.Status);
        Assert.Equal(newsId, updated.PromotedNewsId);

        var entries = await audit.GetByNewsIdAsync(newsId);
        var promote = Assert.Single(entries, entry => entry.Action == "promote-from-discovery");
        Assert.Equal("editor@test.local", promote.ActorEmail);
        Assert.Contains($"#{candidateId}", promote.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromoteDiscoveryCandidateAsync_Throws_WhenCandidateStatusUpdateFails()
    {
        var newsStore = new SharedNewsStore();
        var admin = new InMemoryAdminNewsRepository(newsStore);
        var inner = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDraftedCandidateAsync(inner);
        var discovery = new ConfigurableNewsDiscoveryRepository(inner)
        {
            TryUpdateCandidateStatusHandler = (_, _, _) => Task.FromResult(false),
        };
        var candidate = await inner.GetCandidateByIdAsync(candidateId);
        var agentDraft = await inner.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.NotNull(agentDraft);
        var service = CreateService(admin, discovery, new InMemoryNewsAuditRepository(newsStore));

        var ex = await Assert.ThrowsAsync<AdminNewsPromotionException>(() =>
            service.PromoteDiscoveryCandidateAsync(
                candidate,
                agentDraft,
                NewsDiscoveryPromoteDraft.Build(agentDraft, candidate),
                "editor@test.local"));

        Assert.Equal("Promotion failed while updating the discovery candidate.", ex.Message);
        Assert.Empty(newsStore.GetAuditEntries(1));
    }

    [Fact]
    public async Task PromoteDiscoveryCandidateAsync_WrapsUnexpectedCreateErrors()
    {
        var newsStore = new SharedNewsStore();
        var innerAdmin = new InMemoryAdminNewsRepository(newsStore);
        var admin = new FailingCreateAdminNewsRepository(innerAdmin, new InvalidOperationException("db down"));
        var discovery = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDraftedCandidateAsync(discovery);
        var candidate = await discovery.GetCandidateByIdAsync(candidateId);
        var agentDraft = await discovery.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.NotNull(agentDraft);
        var service = CreateService(admin, discovery, new InMemoryNewsAuditRepository(newsStore));

        var ex = await Assert.ThrowsAsync<AdminNewsPromotionException>(() =>
            service.PromoteDiscoveryCandidateAsync(
                candidate,
                agentDraft,
                NewsDiscoveryPromoteDraft.Build(agentDraft, candidate),
                "editor@test.local"));

        Assert.Contains("creating the admin draft", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PromoteDiscoveryCandidateAsync_PropagatesConcurrencyExceptionUnwrapped()
    {
        var newsStore = new SharedNewsStore();
        var innerAdmin = new InMemoryAdminNewsRepository(newsStore);
        var admin = new FailingCreateAdminNewsRepository(innerAdmin, new OptimisticConcurrencyException());
        var discovery = new InMemoryNewsDiscoveryRepository(new SharedNewsDiscoveryStore());
        var candidateId = await NewsDiscoveryTestSeeder.SeedDraftedCandidateAsync(discovery);
        var candidate = await discovery.GetCandidateByIdAsync(candidateId);
        var agentDraft = await discovery.GetDraftByCandidateIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.NotNull(agentDraft);
        var service = CreateService(admin, discovery, new InMemoryNewsAuditRepository(newsStore));

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            service.PromoteDiscoveryCandidateAsync(
                candidate,
                agentDraft,
                NewsDiscoveryPromoteDraft.Build(agentDraft, candidate),
                "editor@test.local"));
    }

    private static AdminNewsWriteService CreateService(
        IAdminNewsRepository admin,
        INewsDiscoveryRepository discovery,
        INewsAuditRepository audit) =>
        new(
            admin,
            NoOpNotificationDispatcher.Instance,
            new SilentNewsForumTopicService(),
            NullLogger<AdminNewsWriteService>.Instance,
            discovery,
            audit);

    private sealed class SilentNewsForumTopicService : INewsForumTopicService
    {
        public Task EnsureTopicOnFirstPublishAsync(
            AdminNewsArticle article,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
