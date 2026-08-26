using QueenZone.Data.Entities;

namespace QueenZone.Web;

/// <summary>
/// Best-effort APNs/FCM send. One HTTP send per device token. Missing credentials
/// or provider errors must not throw to the caller.
/// </summary>
public interface IPushTransport
{
    Task SendAsync(
        IReadOnlyList<DeviceTokenEntity> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
