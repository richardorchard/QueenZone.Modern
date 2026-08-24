using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

public enum DevicePushPlatform
{
    Apns,
    Fcm,
}

/// <summary>
/// A push token for one mobile device, owned by the member currently signed in on that
/// device. <see cref="DeviceId"/> is the client-supplied stable identifier and is globally
/// unique: re-registering the same device (even under a different member) updates this row
/// rather than creating a duplicate.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeviceTokenEntity
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public Guid MemberAccountId { get; set; }

    public DevicePushPlatform Platform { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public MemberAccount? MemberAccount { get; set; }
}
