using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class SharedNotificationPreferenceStore
{
    public Lock Gate { get; } = new();

    public List<NotificationPreferenceEntity> Rows { get; } = [];
}
