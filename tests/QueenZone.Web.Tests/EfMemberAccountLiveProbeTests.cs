using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in SQL Express mirror probe for member account create + external login uniqueness.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfMemberAccountLiveProbeTests
{
    [Fact]
    public async Task Create_member_and_external_login_when_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var marker = $"member-account-probe-{uniqueSuffix}";
        var email = $"{marker}@queenzone.local";
        var provider = "probe-provider";
        var providerKey = $"probe-key-{uniqueSuffix}";
        Guid? memberId = null;

        try
        {
            await using var context = CreateContext(connectionString);
            var repo = new EfMemberAccountRepository(context);

            var created = await repo.CreateAsync(new MemberAccount
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = $"Member Account Probe {uniqueSuffix}",
                CreatedAt = DateTime.UtcNow,
            });
            memberId = created.Id;
            Assert.Equal(email.ToUpperInvariant(), created.NormalizedEmail);

            await repo.AddExternalLoginAsync(created.Id, provider, providerKey, email);
            var byLogin = await repo.FindByExternalLoginAsync(provider, providerKey);
            Assert.NotNull(byLogin);
            Assert.Equal(created.Id, byLogin.Id);

            var providers = await repo.ListExternalProvidersAsync(created.Id);
            Assert.Contains(provider, providers);

            var renamed = await repo.UpdateDisplayNameAsync(created.Id, $"Member Account Probe Renamed {uniqueSuffix}");
            Assert.NotNull(renamed);
            Assert.Equal($"Member Account Probe Renamed {uniqueSuffix}", renamed.DisplayName);

            await repo.RecordLoginAsync(created.Id, DateTime.UtcNow);
            var afterLogin = await repo.FindByIdAsync(created.Id);
            Assert.NotNull(afterLogin);
            Assert.NotNull(afterLogin.LastLoginAt);
        }
        finally
        {
            await CleanupAsync(connectionString, memberId, marker);
        }
    }

    private static QueenZoneDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                        maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                        errorNumbersToAdd: null);
                })
            .Options;
        return new QueenZoneDbContext(options);
    }

    private static async Task CleanupAsync(string connectionString, Guid? memberId, string marker)
    {
        await using var cleanup = CreateContext(connectionString);
        if (memberId is Guid id)
        {
            await cleanup.MemberExternalLogins
                .Where(login => login.MemberAccountId == id)
                .ExecuteDeleteAsync();
            await cleanup.MemberAccounts
                .Where(m => m.Id == id)
                .ExecuteDeleteAsync();
        }

        await cleanup.MemberAccounts
            .Where(m => m.Email.Contains(marker))
            .ExecuteDeleteAsync();
    }

    private static bool IsProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_MEMBER_ACCOUNT_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
