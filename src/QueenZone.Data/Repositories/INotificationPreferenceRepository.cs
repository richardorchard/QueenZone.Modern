using QueenZone.Data.Entities;

namespace QueenZone.Data;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreferences> GetAsync(
        Guid memberAccountId,
        CancellationToken cancellationToken = default);

    Task<NotificationPreferences> ApplyAsync(
        Guid memberAccountId,
        NotificationPreferencePatch patch,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> FilterEnabledAsync(
        IReadOnlyCollection<Guid> memberAccountIds,
        NotificationCategory category,
        CancellationToken cancellationToken = default);
}
