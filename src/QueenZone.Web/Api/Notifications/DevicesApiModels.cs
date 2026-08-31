namespace QueenZone.Web;

/// <summary>
/// Push provider for a registered mobile device. Wire values stay camelCase
/// <c>apns</c> / <c>fcm</c> via the JSON API enum converter. Named separately from
/// the EF <c>DevicePushPlatform</c> entity enum so schema changes cannot leak into
/// the published contract.
/// </summary>
public enum PushDevicePlatform
{
    Apns,
    Fcm,
}

public sealed record DeviceRegisterRequest(string? DeviceId, PushDevicePlatform? Platform, string? Token);

public sealed record DeviceRegisteredResponse(string DeviceId, PushDevicePlatform Platform, DateTimeOffset UpdatedAt);
