using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryNotificationPreferenceRepository(SharedNotificationPreferenceStore store)
    : INotificationPreferenceRepository
{
    public Task<NotificationPreferences> GetAsync(
        Guid memberAccountId,
        CancellationToken cancellationToken = default)
    {
        lock (store.Gate)
        {
            return Task.FromResult(ResolveLocked(memberAccountId));
        }
    }

    public Task<NotificationPreferences> ApplyAsync(
        Guid memberAccountId,
        NotificationPreferencePatch patch,
        CancellationToken cancellationToken = default)
    {
        lock (store.Gate)
        {
            var now = DateTime.UtcNow;
            UpsertLocked(memberAccountId, NotificationCategory.ForumReply, patch.ForumReply, now);
            UpsertLocked(memberAccountId, NotificationCategory.PrivateMessage, patch.PrivateMessage, now);
            UpsertLocked(memberAccountId, NotificationCategory.News, patch.News, now);
            return Task.FromResult(ResolveLocked(memberAccountId));
        }
    }

    public Task<IReadOnlyList<Guid>> FilterEnabledAsync(
        IReadOnlyCollection<Guid> memberAccountIds,
        NotificationCategory category,
        CancellationToken cancellationToken = default)
    {
        if (memberAccountIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        lock (store.Gate)
        {
            var defaultOn = NotificationPreferences.Defaults.IsEnabled(category);
            var overrides = store.Rows
                .Where(row => row.Category == category)
                .ToDictionary(row => row.MemberAccountId, row => row.IsEnabled);

            var enabled = new List<Guid>();
            var seen = new HashSet<Guid>();
            foreach (var id in memberAccountIds)
            {
                if (!seen.Add(id))
                {
                    continue;
                }

                var on = overrides.TryGetValue(id, out var chosen) ? chosen : defaultOn;
                if (on)
                {
                    enabled.Add(id);
                }
            }

            return Task.FromResult<IReadOnlyList<Guid>>(enabled);
        }
    }

    private NotificationPreferences ResolveLocked(Guid memberAccountId) =>
        NotificationPreferencesMerge.Resolve(
            store.Rows
                .Where(row => row.MemberAccountId == memberAccountId)
                .Select(row => (row.Category, row.IsEnabled)));

    private void UpsertLocked(Guid memberAccountId, NotificationCategory category, bool? enabled, DateTime now)
    {
        if (enabled is null)
        {
            return;
        }

        var existing = store.Rows.FirstOrDefault(
            row => row.MemberAccountId == memberAccountId && row.Category == category);
        if (existing is null)
        {
            store.Rows.Add(new NotificationPreferenceEntity
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
}
