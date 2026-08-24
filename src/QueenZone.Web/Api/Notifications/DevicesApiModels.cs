using QueenZone.Data.Entities;

namespace QueenZone.Web;

public sealed record DeviceRegisterRequest(string? DeviceId, DevicePushPlatform? Platform, string? Token);

public sealed record DeviceRegisteredResponse(string DeviceId, DevicePushPlatform Platform, DateTimeOffset UpdatedAt);
