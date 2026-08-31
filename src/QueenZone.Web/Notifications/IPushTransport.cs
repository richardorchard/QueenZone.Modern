namespace QueenZone.Web;

/// <summary>
/// Transport-facing device token. Mapped from <c>DeviceTokenEntity</c> at the
/// repository boundary so EF schema changes cannot silently alter this contract.
/// </summary>
public sealed record PushDeviceToken(
    Guid MemberAccountId,
    PushDevicePlatform Platform,
    string Token);

/// <summary>
/// Best-effort APNs/FCM send. One HTTP send per device token. Missing credentials
/// or provider errors must not throw to the caller.
/// </summary>
public interface IPushTransport
{
    Task SendAsync(
        IReadOnlyList<PushDeviceToken> tokens,
        PushNotificationPayload payload,
        CancellationToken cancellationToken = default);
}
