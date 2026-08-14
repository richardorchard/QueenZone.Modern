using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class MemberAccountDeletionHostedServiceTests
{
    [Fact]
    public void StartupDelay_is_longer_than_app_service_container_start_limit()
    {
        Assert.True(
            MemberAccountDeletionHostedService.DefaultStartupDelay > TimeSpan.FromSeconds(230));
        Assert.Equal(TimeSpan.FromMinutes(5), MemberAccountDeletionHostedService.DefaultStartupDelay);
    }

    [Fact]
    public async Task Does_not_purge_during_startup_delay()
    {
        var repository = new RecordingPurgeRepository();
        using var hosted = CreateHostedService(repository, startupDelay: TimeSpan.FromMinutes(5));

        await hosted.StartAsync(CancellationToken.None);
        await Task.Delay(80);
        await hosted.StopAsync(CancellationToken.None);

        Assert.Equal(0, repository.PurgeCalls);
    }

    [Fact]
    public async Task Purges_after_startup_delay()
    {
        var repository = new RecordingPurgeRepository();
        using var hosted = CreateHostedService(
            repository,
            startupDelay: TimeSpan.FromMilliseconds(20),
            runInterval: Timeout.InfiniteTimeSpan);

        await hosted.StartAsync(CancellationToken.None);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (repository.PurgeCalls == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        await hosted.StopAsync(CancellationToken.None);

        Assert.Equal(1, repository.PurgeCalls);
    }

    [Fact]
    public async Task Stop_during_startup_delay_does_not_purge()
    {
        var repository = new RecordingPurgeRepository();
        using var hosted = CreateHostedService(repository, startupDelay: TimeSpan.FromHours(1));

        await hosted.StartAsync(CancellationToken.None);
        await hosted.StopAsync(CancellationToken.None);

        Assert.Equal(0, repository.PurgeCalls);
    }

    private static MemberAccountDeletionHostedService CreateHostedService(
        RecordingPurgeRepository repository,
        TimeSpan startupDelay,
        TimeSpan? runInterval = null)
    {
        var memberService = new MemberAccountService(
            repository,
            new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>()),
            new AzureBlobUploadService(
                new InMemoryBlobStorageBackend(),
                Options.Create(new BlobUploadOptions())),
            new MemberUploadQuotaService(
                new Microsoft.Extensions.Caching.Memory.MemoryCache(
                    new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                TimeProvider.System,
                Options.Create(new UploadQuotaOptions { Enabled = false })));

        var services = new ServiceCollection();
        services.AddSingleton(memberService);
        var provider = services.BuildServiceProvider();

        return new MemberAccountDeletionHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<MemberAccountDeletionHostedService>.Instance)
        {
            StartupDelay = startupDelay,
            RunInterval = runInterval ?? MemberAccountDeletionHostedService.DefaultRunInterval,
        };
    }

    private sealed class RecordingPurgeRepository : IMemberAccountRepository
    {
        public int PurgeCalls;

        public Task<MemberAccountDeletionPurgeResult> PurgeDeletedAccountsAsync(
            DateTime purgeBefore,
            DateTime purgedAt,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref PurgeCalls);
            return Task.FromResult(new MemberAccountDeletionPurgeResult(0, []));
        }

        public Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> FindByExternalLoginAsync(
            string provider,
            string providerKey,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListExternalProvidersAsync(
            Guid memberAccountId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddExternalLoginAsync(
            Guid memberAccountId,
            string provider,
            string providerKey,
            string email,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> UpdateDisplayNameAsync(
            Guid memberId,
            string displayName,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> UpdateAvatarUrlAsync(
            Guid memberId,
            string? avatarBlobPath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> FindByLinkedLegacyUserIdAsync(
            int legacyUserId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> LinkLegacyUserIdAsync(
            Guid memberId,
            int legacyUserId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> UnlinkLegacyUserIdAsync(
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RecordLoginAsync(
            Guid memberId,
            DateTime loginAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(
            int count,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(
            DateOnly fromDate,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MemberRecipientMatch>> SearchByDisplayNameAsync(
            string query,
            Guid? excludeMemberId = null,
            int maxResults = PrivateMessageLimits.MaxRecipientSearchResults,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberSearchResult> SearchMembersAsync(
            string? query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> SuspendAsync(
            Guid memberId,
            string reason,
            string suspendedByAdminEmail,
            DateTime suspendedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> ReinstateAsync(
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccountDeletionRequestResult?> RequestDeletionAsync(
            Guid memberId,
            DateTime requestedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MemberAccount?> CancelDeletionAsync(
            Guid memberId,
            DateTime cancelledAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
