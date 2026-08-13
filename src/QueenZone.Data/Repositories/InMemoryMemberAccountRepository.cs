using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryMemberAccountRepository : IMemberAccountRepository
{
    private readonly List<MemberAccount> accounts = [];
    private readonly List<MemberExternalLogin> externalLogins = [];
    private readonly Lock gate = new();

    private readonly List<MemberAccountDeletionAuditLogEntity> deletionAuditLogs = [];

    public Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.NormalizedEmail == Normalize(email));
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == id);
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var login = externalLogins.FirstOrDefault(l => l.Provider == provider && l.ProviderKey == providerKey);
            var account = login is null ? null : accounts.FirstOrDefault(a => a.Id == login.MemberAccountId);
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            account.NormalizedEmail = Normalize(account.Email);
            accounts.Add(account);
            return Task.FromResult(account);
        }
    }

    public Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            externalLogins.Add(new MemberExternalLogin
            {
                Id = Guid.NewGuid(),
                MemberAccountId = memberAccountId,
                Provider = provider,
                ProviderKey = providerKey,
                Email = email,
                LinkedAt = DateTime.UtcNow,
            });
            return Task.CompletedTask;
        }
    }

    public Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<string> providers = externalLogins
                .Where(login => login.MemberAccountId == memberAccountId)
                .Select(login => login.Provider)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(provider => provider, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return Task.FromResult(providers);
        }
    }

    public Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.DisplayName = displayName;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.AvatarUrl = avatarBlobPath;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccount?> FindByLinkedLegacyUserIdAsync(
        int legacyUserId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.LinkedLegacyUserId == legacyUserId);
            return Task.FromResult(account);
        }
    }

    public Task<MemberAccount?> LinkLegacyUserIdAsync(
        Guid memberId,
        int legacyUserId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            if (account.LinkedLegacyUserId is null)
            {
                account.LinkedLegacyUserId = legacyUserId;
            }

            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccount?> UnlinkLegacyUserIdAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.LinkedLegacyUserId = null;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is not null)
            {
                account.LastLoginAt = loginAt;
            }

            return Task.CompletedTask;
        }
    }

    public Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var today = utcNow.Date;
            var stats = new MemberStats(
                Total: accounts.Count,
                NewToday: accounts.Count(a => a.CreatedAt >= today),
                NewLast7Days: accounts.Count(a => a.CreatedAt >= today.AddDays(-7)),
                NewLast30Days: accounts.Count(a => a.CreatedAt >= today.AddDays(-30)));
            return Task.FromResult(stats);
        }
    }

    public Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            IReadOnlyList<RecentLogin> logins = accounts
                .Where(a => a.LastLoginAt != null)
                .OrderByDescending(a => a.LastLoginAt)
                .Take(count)
                .Select(a => new RecentLogin(a.Id, a.DisplayName, a.AvatarUrl != null, a.LastLoginAt!.Value))
                .ToList();
            return Task.FromResult(logins);
        }
    }

    public Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var from = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            IReadOnlyList<DailyRegistration> regs = accounts
                .Where(a => a.CreatedAt >= from)
                .GroupBy(a => DateOnly.FromDateTime(a.CreatedAt))
                .Select(g => new DailyRegistration(g.Key, g.Count()))
                .OrderBy(r => r.Date)
                .ToList();
            return Task.FromResult(regs);
        }
    }

    public Task<IReadOnlyList<MemberRecipientMatch>> SearchByDisplayNameAsync(
        string query,
        Guid? excludeMemberId = null,
        int maxResults = PrivateMessageLimits.MaxRecipientSearchResults,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<MemberRecipientMatch>>([]);
        }

        maxResults = Math.Clamp(maxResults, 1, PrivateMessageLimits.MaxRecipientSearchResults);
        var term = query.Trim();

        lock (gate)
        {
            IReadOnlyList<MemberRecipientMatch> matches = accounts
                .Where(account =>
                    account.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    && account.DeletionRequestedAt is null
                    && (excludeMemberId is null || account.Id != excludeMemberId.Value))
                .OrderBy(account => account.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(maxResults)
                .Select(account => new MemberRecipientMatch(account.Id, account.DisplayName))
                .ToList();
            return Task.FromResult(matches);
        }
    }

    public Task<MemberSearchResult> SearchMembersAsync(
        string? query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var term = query?.Trim();

        lock (gate)
        {
            IEnumerable<MemberAccount> matches = accounts;
            if (!string.IsNullOrWhiteSpace(term))
            {
                matches = matches.Where(account =>
                    account.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || account.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = matches.OrderByDescending(account => account.CreatedAt).ToList();
            var page = ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult(new MemberSearchResult(page, ordered.Count));
        }
    }

    public Task<MemberAccount?> SuspendAsync(
        Guid memberId,
        string reason,
        string suspendedByAdminEmail,
        DateTime suspendedAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.IsSuspended = true;
            account.SuspendedAt = suspendedAt;
            account.SuspendedReason = reason;
            account.SuspendedByAdminEmail = suspendedByAdminEmail;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccount?> ReinstateAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccount?>(null);
            }

            account.IsSuspended = false;
            account.SuspendedAt = null;
            account.SuspendedReason = null;
            account.SuspendedByAdminEmail = null;
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccountDeletionRequestResult?> RequestDeletionAsync(
        Guid memberId,
        DateTime requestedAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null)
            {
                return Task.FromResult<MemberAccountDeletionRequestResult?>(null);
            }

            if (account.DeletionRequestedAt is not null)
            {
                return Task.FromResult<MemberAccountDeletionRequestResult?>(
                    new(account, AlreadyRequested: true));
            }

            account.DeletionRecoveryDisplayName = account.DisplayName;
            account.DeletionRecoveryAvatarUrl = account.AvatarUrl;
            account.DisplayName = MemberAccountDeletionPolicy.DeletedDisplayName;
            account.AvatarUrl = null;
            account.DeletionRequestedAt = requestedAt;
            deletionAuditLogs.Add(new MemberAccountDeletionAuditLogEntity
            {
                MemberAccountId = memberId,
                Action = MemberAccountDeletionPolicy.RequestedAuditAction,
                OccurredAt = requestedAt,
            });

            return Task.FromResult<MemberAccountDeletionRequestResult?>(
                new(account, AlreadyRequested: false));
        }
    }

    public Task<MemberAccount?> CancelDeletionAsync(
        Guid memberId,
        DateTime cancelledAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == memberId);
            if (account is null || account.DeletionRequestedAt is null || account.PersonalDataPurgedAt is not null)
            {
                return Task.FromResult(account);
            }

            if (cancelledAt >= account.DeletionRequestedAt.Value.AddDays(MemberAccountDeletionPolicy.RetentionDays))
            {
                return Task.FromResult<MemberAccount?>(account);
            }

            account.DisplayName = account.DeletionRecoveryDisplayName ?? MemberAccountDeletionPolicy.DeletedDisplayName;
            account.AvatarUrl = account.DeletionRecoveryAvatarUrl;
            account.DeletionRequestedAt = null;
            account.DeletionRecoveryDisplayName = null;
            account.DeletionRecoveryAvatarUrl = null;
            deletionAuditLogs.Add(new MemberAccountDeletionAuditLogEntity
            {
                MemberAccountId = memberId,
                Action = MemberAccountDeletionPolicy.CancelledAuditAction,
                OccurredAt = cancelledAt,
            });
            return Task.FromResult<MemberAccount?>(account);
        }
    }

    public Task<MemberAccountDeletionPurgeResult> PurgeDeletedAccountsAsync(
        DateTime purgeBefore,
        DateTime purgedAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var dueAccounts = accounts
                .Where(account =>
                    account.DeletionRequestedAt is not null
                    && account.DeletionRequestedAt <= purgeBefore
                    && account.PersonalDataPurgedAt is null)
                .ToList();
            var avatarBlobPaths = dueAccounts
                .Select(account => account.DeletionRecoveryAvatarUrl)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
                .ToList();

            foreach (var account in dueAccounts)
            {
                externalLogins.RemoveAll(login => login.MemberAccountId == account.Id);
                var deletedEmail = MemberAccountDeletionPolicy.CreateDeletedEmail(account.Id);
                account.Email = deletedEmail;
                account.NormalizedEmail = Normalize(deletedEmail);
                account.DisplayName = MemberAccountDeletionPolicy.DeletedDisplayName;
                account.AvatarUrl = null;
                account.DeletionRecoveryDisplayName = null;
                account.DeletionRecoveryAvatarUrl = null;
                account.PasswordHash = null;
                account.LastLoginAt = null;
                account.IsSuspended = true;
                account.SuspendedAt = purgedAt;
                account.SuspendedReason = null;
                account.SuspendedByAdminEmail = null;
                account.PersonalDataPurgedAt = purgedAt;
                deletionAuditLogs.Add(new MemberAccountDeletionAuditLogEntity
                {
                    MemberAccountId = account.Id,
                    Action = MemberAccountDeletionPolicy.PurgedAuditAction,
                    OccurredAt = purgedAt,
                });
            }

            return Task.FromResult(new MemberAccountDeletionPurgeResult(dueAccounts.Count, avatarBlobPaths));
        }
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
