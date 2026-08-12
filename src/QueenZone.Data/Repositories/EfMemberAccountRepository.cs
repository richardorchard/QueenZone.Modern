using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfMemberAccountRepository(QueenZoneDbContext dbContext) : IMemberAccountRepository
{
    public async Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.NormalizedEmail == Normalize(email), cancellationToken);

    public async Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.Id == id, cancellationToken);

    public async Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        var login = await dbContext.MemberExternalLogins
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey, cancellationToken);

        return login is null ? null : await FindByIdAsync(login.MemberAccountId, cancellationToken);
    }

    public async Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default)
    {
        account.NormalizedEmail = Normalize(account.Email);
        dbContext.MemberAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default)
    {
        dbContext.MemberExternalLogins.Add(new MemberExternalLogin
        {
            Id = Guid.NewGuid(),
            MemberAccountId = memberAccountId,
            Provider = provider,
            ProviderKey = providerKey,
            Email = email,
            LinkedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default) =>
        await dbContext.MemberExternalLogins
            .AsNoTracking()
            .Where(login => login.MemberAccountId == memberAccountId)
            .Select(login => login.Provider)
            .Distinct()
            .OrderBy(provider => provider)
            .ToListAsync(cancellationToken);

    public async Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default)
    {
        // Load tracked so change detection persists the new name.
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.DisplayName = displayName;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.AvatarUrl = avatarBlobPath;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<MemberAccount?> FindByLinkedLegacyUserIdAsync(
        int legacyUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.LinkedLegacyUserId == legacyUserId, cancellationToken);

    public async Task<MemberAccount?> LinkLegacyUserIdAsync(
        Guid memberId,
        int legacyUserId,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (account.LinkedLegacyUserId is not null)
        {
            return account;
        }

        account.LinkedLegacyUserId = legacyUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<MemberAccount?> UnlinkLegacyUserIdAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        if (account.LinkedLegacyUserId is not null)
        {
            account.LinkedLegacyUserId = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return account;
    }

    public async Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is not null)
        {
            account.LastLoginAt = loginAt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var today = utcNow.Date;
        var sevenDaysAgo = today.AddDays(-7);
        var thirtyDaysAgo = today.AddDays(-30);

        var total = await dbContext.MemberAccounts.CountAsync(cancellationToken);
        var newToday = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= today, cancellationToken);
        var newLast7 = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= sevenDaysAgo, cancellationToken);
        var newLast30 = await dbContext.MemberAccounts.CountAsync(a => a.CreatedAt >= thirtyDaysAgo, cancellationToken);

        return new MemberStats(total, newToday, newLast7, newLast30);
    }

    public async Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(a => a.LastLoginAt != null)
            .OrderByDescending(a => a.LastLoginAt)
            .Take(count)
            .Select(a => new RecentLogin(a.Id, a.DisplayName, a.AvatarUrl != null, a.LastLoginAt!.Value))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rawDates = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(a => a.CreatedAt >= from)
            .Select(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return rawDates
            .GroupBy(d => DateOnly.FromDateTime(d))
            .Select(g => new DailyRegistration(g.Key, g.Count()))
            .OrderBy(r => r.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<MemberRecipientMatch>> SearchByDisplayNameAsync(
        string query,
        Guid? excludeMemberId = null,
        int maxResults = PrivateMessageLimits.MaxRecipientSearchResults,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        maxResults = Math.Clamp(maxResults, 1, PrivateMessageLimits.MaxRecipientSearchResults);
        var term = query.Trim();

        var rows = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(account =>
                account.DisplayName.Contains(term)
                && account.DeletionRequestedAt == null
                && (excludeMemberId == null || account.Id != excludeMemberId))
            .OrderBy(account => account.DisplayName)
            .Take(maxResults)
            .Select(account => new MemberRecipientMatch(account.Id, account.DisplayName))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<MemberSearchResult> SearchMembersAsync(
        string? query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var term = query?.Trim();

        var matches = dbContext.MemberAccounts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(term))
        {
            matches = matches.Where(account =>
                account.DisplayName.Contains(term) || account.Email.Contains(term));
        }

        var totalCount = await matches.CountAsync(cancellationToken);
        var members = await matches
            .OrderByDescending(account => account.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new MemberSearchResult(members, totalCount);
    }

    public async Task<MemberAccount?> SuspendAsync(
        Guid memberId,
        string reason,
        string suspendedByAdminEmail,
        DateTime suspendedAt,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.IsSuspended = true;
        account.SuspendedAt = suspendedAt;
        account.SuspendedReason = reason;
        account.SuspendedByAdminEmail = suspendedByAdminEmail;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<MemberAccount?> ReinstateAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.MemberAccounts
            .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        account.IsSuspended = false;
        account.SuspendedAt = null;
        account.SuspendedReason = null;
        account.SuspendedByAdminEmail = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    public Task<MemberAccountDeletionRequestResult?> RequestDeletionAsync(
        Guid memberId,
        DateTime requestedAt,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync<MemberAccountDeletionRequestResult?>(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var account = await dbContext.MemberAccounts
                .SingleOrDefaultAsync(a => a.Id == memberId, cancellationToken);
            if (account is null)
            {
                return null;
            }

            if (account.DeletionRequestedAt is not null)
            {
                return new MemberAccountDeletionRequestResult(account, null, AlreadyRequested: true);
            }

            var previousAvatarUrl = account.AvatarUrl;
            var starterThreadIds = dbContext.ModernForumPosts
                .Where(post =>
                    post.AuthorMemberId == memberId
                    && !dbContext.ModernForumPosts.Any(other =>
                        other.ThreadId == post.ThreadId
                        && other.LegacyPostId < post.LegacyPostId))
                .Select(post => post.ThreadId);

            await dbContext.ModernForumThreads
                .Where(thread => starterThreadIds.Contains(thread.Id))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(thread => thread.StartedByDisplayName, MemberAccountDeletionPolicy.DeletedDisplayName)
                        .SetProperty(thread => thread.UpdatedAt, requestedAt),
                    cancellationToken);

            await dbContext.ModernForumPosts
                .Where(post => post.AuthorMemberId == memberId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(post => post.AuthorMemberId, (Guid?)null)
                        .SetProperty(post => post.AuthorDisplayName, MemberAccountDeletionPolicy.DeletedDisplayName)
                        .SetProperty(post => post.UpdatedAt, requestedAt),
                    cancellationToken);

            var articleSourceKeys = dbContext.ArticleSubmissions
                .Where(article => article.AuthorMemberId == memberId)
                .Select(article => "article:" + article.Slug);
            await dbContext.SearchDocuments
                .Where(document => articleSourceKeys.Contains(document.SourceKey))
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        document => document.AuthorDisplayName,
                        MemberAccountDeletionPolicy.DeletedDisplayName),
                    cancellationToken);

            account.DisplayName = MemberAccountDeletionPolicy.DeletedDisplayName;
            account.AvatarUrl = null;
            account.IsSuspended = true;
            account.SuspendedAt = requestedAt;
            account.SuspendedReason = "Account deletion requested";
            account.SuspendedByAdminEmail = null;
            account.DeletionRequestedAt = requestedAt;

            dbContext.MemberAccountDeletionAuditLogs.Add(new MemberAccountDeletionAuditLogEntity
            {
                MemberAccountId = memberId,
                Action = MemberAccountDeletionPolicy.RequestedAuditAction,
                OccurredAt = requestedAt,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new MemberAccountDeletionRequestResult(account, previousAvatarUrl, AlreadyRequested: false);
        });
    }

    public Task<int> PurgeDeletedAccountsAsync(
        DateTime purgeBefore,
        DateTime purgedAt,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var accounts = await dbContext.MemberAccounts
                .Where(account =>
                    account.DeletionRequestedAt != null
                    && account.DeletionRequestedAt <= purgeBefore
                    && account.PersonalDataPurgedAt == null)
                .ToListAsync(cancellationToken);
            if (accounts.Count == 0)
            {
                await transaction.CommitAsync(cancellationToken);
                return 0;
            }

            var memberIds = accounts.Select(account => account.Id).ToList();
            await dbContext.MemberExternalLogins
                .Where(login => memberIds.Contains(login.MemberAccountId))
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var account in accounts)
            {
                var deletedEmail = MemberAccountDeletionPolicy.CreateDeletedEmail(account.Id);
                account.Email = deletedEmail;
                account.NormalizedEmail = Normalize(deletedEmail);
                account.DisplayName = MemberAccountDeletionPolicy.DeletedDisplayName;
                account.AvatarUrl = null;
                account.PasswordHash = null;
                account.LastLoginAt = null;
                account.SuspendedReason = null;
                account.SuspendedByAdminEmail = null;
                account.PersonalDataPurgedAt = purgedAt;

                dbContext.MemberAccountDeletionAuditLogs.Add(new MemberAccountDeletionAuditLogEntity
                {
                    MemberAccountId = account.Id,
                    Action = MemberAccountDeletionPolicy.PurgedAuditAction,
                    OccurredAt = purgedAt,
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return accounts.Count;
        });
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
