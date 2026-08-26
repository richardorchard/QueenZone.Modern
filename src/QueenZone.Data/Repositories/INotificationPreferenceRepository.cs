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

    /// <summary>
    /// Members with an explicit enabled row for <paramref name="category"/>.
    /// Does not apply defaults — missing rows are omitted. Use this for
    /// <see cref="NotificationCategory.News"/> fan-out (default is off).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListEnabledAsync(
        NotificationCategory category,
        CancellationToken cancellationToken = default);
}
