using QueenZone.Data.Entities;
using QueenZone.Web;
using EntityPlatform = QueenZone.Data.Entities.DevicePushPlatform;

namespace QueenZone.Web.Tests;

public sealed class DeviceTokenMapperTests
{
    [Fact]
    public void ToEntity_AndToRegisteredResponse_RoundTripPlatformWithoutToken()
    {
        var memberId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var now = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        var entity = DeviceTokenMapper.ToEntity(
            memberId,
            "device-1",
            PushDevicePlatform.Apns,
            "secret-token",
            now);

        Assert.Equal("device-1", entity.DeviceId);
        Assert.Equal(memberId, entity.MemberAccountId);
        Assert.Equal(EntityPlatform.Apns, entity.Platform);
        Assert.Equal("secret-token", entity.Token);
        Assert.Equal(now, entity.CreatedAt);
        Assert.Equal(now, entity.UpdatedAt);

        var response = DeviceTokenMapper.ToRegisteredResponse(entity);
        Assert.Equal("device-1", response.DeviceId);
        Assert.Equal(PushDevicePlatform.Apns, response.Platform);
        Assert.Equal(new DateTimeOffset(now), response.UpdatedAt);
    }

    [Fact]
    public void ToPushTokens_MapsEntityFieldsUsedByTransport()
    {
        var memberId = Guid.NewGuid();
        var entity = DeviceTokenTestData.Token(memberId, EntityPlatform.Fcm, "fcm-secret");

        var mapped = Assert.Single(DeviceTokenMapper.ToPushTokens([entity]));
        Assert.Equal(memberId, mapped.MemberAccountId);
        Assert.Equal(PushDevicePlatform.Fcm, mapped.Platform);
        Assert.Equal("fcm-secret", mapped.Token);
    }

    [Theory]
    [InlineData(EntityPlatform.Apns, PushDevicePlatform.Apns)]
    [InlineData(EntityPlatform.Fcm, PushDevicePlatform.Fcm)]
    public void PlatformMapping_IsSymmetric(EntityPlatform entity, PushDevicePlatform api)
    {
        Assert.Equal(api, DeviceTokenMapper.ToApiPlatform(entity));
        Assert.Equal(entity, DeviceTokenMapper.ToEntityPlatform(api));
    }
}
