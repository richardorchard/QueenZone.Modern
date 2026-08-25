using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfNotificationPreferenceRepository(QueenZoneDbContext dbContext)
    : INotificationPreferenceRepository
{
    public async Task<NotificationPreferences> GetAsync(
        Guid memberAccountId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(row => row.MemberAccountId == memberAccountId)
            .ToListAsync(cancellationToken);

        return NotificationPreferencesMerge.Resolve(rows.Select(ToChoice));
    }

    public async Task<NotificationPreferences> ApplyAsync(
        Guid memberAccountId,
        NotificationPreferencePatch patch,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await UpsertIfSetAsync(memberAccountId, NotificationCategory.ForumReply, patch.ForumReply, now, cancellationToken);
        await UpsertIfSetAsync(memberAccountId, NotificationCategory.PrivateMessage, patch.PrivateMessage, now, cancellationToken);
        await UpsertIfSetAsync(memberAccountId, NotificationCategory.News, patch.News, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(memberAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> FilterEnabledAsync(
        IReadOnlyCollection<Guid> memberAccountIds,
        NotificationCategory category,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountIds.Count == 0)
        {
            return [];
        }

        var ids = memberAccountIds.Distinct().ToArray();
        var rows = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(row => ids.Contains(row.MemberAccountId) && row.Category == category)
            .Select(row => new { row.MemberAccountId, row.IsEnabled })
            .ToListAsync(cancellationToken);

        var overrides = rows.ToDictionary(row => row.MemberAccountId, row => row.IsEnabled);
        var defaultOn = NotificationPreferences.Defaults.IsEnabled(category);
        return ids.Where(id => overrides.TryGetValue(id, out var enabled) ? enabled : defaultOn).ToArray();
    }

    private async Task UpsertIfSetAsync(
        Guid memberAccountId,
        NotificationCategory category,
        bool? enabled,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (enabled is null)
        {
            return;
        }

        var existing = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(
                row => row.MemberAccountId == memberAccountId && row.Category == category,
                cancellationToken);

        if (existing is null)
        {
            dbContext.NotificationPreferences.Add(new NotificationPreferenceEntity
            {
                MemberAccountId = memberAccountId,
                Category = category,
                IsEnabled = enabled.Value,
                UpdatedAt = now,
            });
            return;
        }

        existing.IsEnabled = enabled.Value;
        existing.UpdatedAt = now;
    }

    private static (NotificationCategory Category, bool Enabled) ToChoice(NotificationPreferenceEntity row) =>
        (row.Category, row.IsEnabled);
}
