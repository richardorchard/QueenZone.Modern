using QueenZone.Data.Entities;
using EntityPlatform = QueenZone.Data.Entities.DevicePushPlatform;

namespace QueenZone.Web;

public static class DeviceTokenMapper
{
    public static DeviceTokenEntity ToEntity(
        Guid memberAccountId,
        string deviceId,
        PushDevicePlatform platform,
        string token,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            MemberAccountId = memberAccountId,
            Platform = ToEntityPlatform(platform),
            Token = token,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

    public static DeviceRegisteredResponse ToRegisteredResponse(DeviceTokenEntity stored) =>
        new(stored.DeviceId, ToApiPlatform(stored.Platform), ToUtc(stored.UpdatedAt));

    public static PushDeviceToken ToPushToken(DeviceTokenEntity token) =>
        new(token.MemberAccountId, ToApiPlatform(token.Platform), token.Token);

    public static IReadOnlyList<PushDeviceToken> ToPushTokens(IEnumerable<DeviceTokenEntity> tokens) =>
        tokens.Select(ToPushToken).ToList();

    public static PushDevicePlatform ToApiPlatform(EntityPlatform platform) =>
        platform switch
        {
            EntityPlatform.Apns => PushDevicePlatform.Apns,
            EntityPlatform.Fcm => PushDevicePlatform.Fcm,
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
        };

    public static EntityPlatform ToEntityPlatform(PushDevicePlatform platform) =>
        platform switch
        {
            PushDevicePlatform.Apns => EntityPlatform.Apns,
            PushDevicePlatform.Fcm => EntityPlatform.Fcm,
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
        };

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
